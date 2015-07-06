using Models;
using ProtoBuf;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BusinessLogic
{
    /// <summary>
    /// Test
    /// </summary>
    public class UserProvider
    {
        private const string UniqueIdKey = "uniqueIdKey";
        private const string UniquePostKey = "uniquePostKey";

        private const string UserIndexFormat = "user:{0}";
        private const string FollowingIndexFormat = "following:{0}";
        private const string FollowersIndexFormat = "followers:{0}";

        private const string PostsIndexFormat = "posts:{0}";

        private const string PostIndexFormat = "post:{0}";

        private const string PublicTimelineKey = "publicTimeline";

        private const string UserHashSet = "users";
        private const string UsernameField = "username";
        private const string PasswordField = "password";

        public async Task<long> AddNewUser(string username, string password)
        {
            // Get a unique ID
            long newId = await GetNewUserId();

            // Add the user information into redis
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Index by ID
            await redisDb.HashSetAsync(string.Format(UserIndexFormat, newId), new HashEntry[] {
                new HashEntry(UsernameField, username),
                new HashEntry(PasswordField, password)
            });

            // Index by Username
            await redisDb.HashSetAsync(UserHashSet, username, newId);

            return newId;
        }

        public async Task<bool> IsUsernameAvailable(string username)
        {
            return (-1 == await GetUserId(username));
        }

        public async Task<long> GetUserId(string username)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();
            var value = await redisDb.HashGetAsync(UserHashSet, username);

            if (value.IsNull)
            {
                return -1;
            }
            else
            {
                return (long)value;
            }
        }

        public async Task<string> GetUsername(long id)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();
            var value = await redisDb.HashGetAsync(string.Format(UserIndexFormat, id), UsernameField);
            if (value.IsNull)
            {
                return string.Empty;
            }
            else
            {
                return (string)value;
            }
        }

        public async Task<bool> FollowUser(long actorId, string target)
        {
            bool success = true;

            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Get both user Ids
            var targetId = await redisDb.HashGetAsync(UserHashSet, target);

            // Calculate the follow time
            var followTime = DateTime.UtcNow.Ticks;

            success &= !targetId.IsNull;

            // Store in the actors list of follows
            if (success)
            {
                await redisDb.SortedSetAddAsync(string.Format(FollowingIndexFormat, actorId), new SortedSetEntry[] {
                    new SortedSetEntry(targetId, followTime)});
                await redisDb.SortedSetAddAsync(string.Format(FollowersIndexFormat, targetId), new SortedSetEntry[] {
                    new SortedSetEntry(actorId, followTime)});
            }

            return success;
        }

        public async Task<long> Post(Post post)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            var postId = await GetNewPostId();

            // Serialize the post
            var serializedPost = SerializationUtils.Serialize(post);

            // Add the post indexed by Id
            await redisDb.StringSetAsync(string.Format(PostIndexFormat, postId), serializedPost);

            var postTicks = post.PostTime.Ticks;

            // Add the post ID to the owners post list
            await redisDb.SortedSetAddAsync(string.Format(PostsIndexFormat, post.PostOwnerId), new SortedSetEntry[] {
                new SortedSetEntry(postId, postTicks)
            });

            // Add the post ID to the public timeline
            await redisDb.SortedSetAddAsync(PublicTimelineKey, new SortedSetEntry[] {
                new SortedSetEntry(postId, postTicks)
            });

            return postId;
        }

        public async Task<Post[]> GetPostsFromFollows(long userId)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Get all of the posters this user follows
            var following = await redisDb.SortedSetRangeByRankAsync(string.Format(FollowingIndexFormat, userId), 0, -1);

            // Get the posts from all of the users
            ConcurrentBag<Post> posts = new ConcurrentBag<Post>();
            List<Task> postFetchTasks = new List<Task>(following.Length);
            foreach (RedisValue poster in following)
            {
                postFetchTasks.Add(GetUserPosts(poster).ContinueWith(postFetchTask =>
                    {
                        foreach (Post post in postFetchTask.Result)
                        {
                            posts.Add(post);
                        }
                    }));
            }

            await Task.WhenAll(postFetchTasks);

            // Sort by descending
            return posts.OrderByDescending(post => post.PostTime).ToArray();
        }

        public async Task<Post[]> GetUserPosts(RedisValue user)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Get the post Ids from the users timeline
            var postIds = await redisDb.SortedSetRangeByRankAsync(string.Format(PostsIndexFormat, user));

            return await GetPostsFromIds(postIds);
        }

        public async Task<Post[]> GetPublicTimeline()
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Get the post Ids from the public timeline
            var postIds = await redisDb.SortedSetRangeByRankAsync(PublicTimelineKey);

            return await GetPostsFromIds(postIds);
        }

        private async Task<Post[]> GetPostsFromIds(RedisValue[] postIds)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();
            ConcurrentBag<Post> posts = new ConcurrentBag<Post>();
            List<Task> getPostTasks = new List<Task>(postIds.Length);
            foreach (long postId in postIds)
            {
                getPostTasks.Add(redisDb.StringGetAsync(string.Format(PostIndexFormat, postId)).ContinueWith(postValueTask =>
                {
                    byte[] serializedPost = (byte[])postValueTask.Result;
                    var deserializedPost = SerializationUtils.Deserialize<Post>(serializedPost);
                    if (deserializedPost != null)
                    {
                        posts.Add(deserializedPost);
                    }
                }));
            }
            await Task.WhenAll(getPostTasks.ToArray());

            // Sort by descending
            return posts.OrderByDescending(post => post.PostTime).ToArray();
        }

        public async Task<long> Login(string username, string password)
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Get the user Id
            var userId = await redisDb.HashGetAsync(UserHashSet, username);

            if (userId.IsNull)
            {
                // user does not exist, login fails
                return -1;
            }
            else
            {
                var actualPassword = await redisDb.HashGetAsync(string.Format(UserIndexFormat, userId), PasswordField);

                if (string.Equals(password, actualPassword))
                {
                    return (long)userId;
                }
            }

            return -1;
        }

        public async Task<long> GetNewUserId()
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Return a new id
            return await redisDb.StringIncrementAsync(UniqueIdKey);
        }

        public async Task<long> GetNewPostId()
        {
            var redisDb = Dependencies.Instance.Value.GetRedisDatabase();

            // Return a new id
            return await redisDb.StringIncrementAsync(UniquePostKey);
        }
    }
}