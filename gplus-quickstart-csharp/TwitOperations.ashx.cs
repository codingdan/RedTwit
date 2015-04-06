using RedisBoost;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Compilation;
using System.Web.Routing;
using System.Web.SessionState;

namespace GPlusQuickstartCsharp
{
    /// <summary>
    /// Summary description for TwitOperations
    /// </summary>
    public class TwitOperations : IHttpHandler, IRouteHandler, IRequiresSessionState
    {

        public async void ProcessRequest(HttpContext context)
        {
            IDatabase redisDb = Dependencies.Instance.Value.GetRedisDatabase();
            if (context.Request.Path.Contains("saveData"))
            {
                string key = context.Request.Form["key"];
                string value = context.Request.Form["value"];
                await redisDb.StringSetAsync(key, value);

                context.Response.ContentType = "text/plain";
                context.Response.Write(string.Format("Saved {0} with value {1}", key, value));
            }
            else if (context.Request.Path.Contains("getData"))
            {
                string key = context.Request.Form["key"];
                string cachedValue = await redisDb.StringGetAsync(key);

                context.Response.ContentType = "text/plain";
                context.Response.Write(string.Format("Found {0} with value {1}", key, cachedValue));
            }
            else if (context.Request.Path.Contains("loginUser"))
            {
                UserProvider userProvider = new UserProvider();

                // Attempt a login
                var loginResult = await userProvider.Login(
                    context.Request.Form["username"],
                    context.Request.Form["password"]);

                context.Response.ContentType = "text/plain";
                if (loginResult != -1)
                {
                    HttpSessionState sessionState = context.Session;
                    sessionState["UserId"] = loginResult;
                    context.Response.Write("Login successful");
                }
                else
                {
                    context.Response.Write("Login failed");
                }
            }
            else if (context.Request.Path.Contains("createUser"))
            {
                UserProvider userProvider = new UserProvider();

                context.Response.ContentType = "text/plain";
                // See if the user is available
                if (!await userProvider.IsUsernameAvailable(context.Request.Form["username"]))
                {
                    context.Response.Write("username already exists");
                }
                else
                {
                    var newUserId = await userProvider.AddNewUser(
                        context.Request.Form["username"],
                        context.Request.Form["password"]);

                    context.Response.Write(string.Format("created username with id: {0}", newUserId));
                }
            }
            else if (context.Request.Path.Contains("post"))
            {
                UserProvider userProvider = new UserProvider();

                context.Response.ContentType = "text/plain";
                HttpSessionState sessionState = context.Session;
                if (context.Session == null || sessionState["UserId"] == null)
                {
                    context.Response.Write("No user logged in on this browser");
                }
                else
                {
                    // See if the user is available
                    var postId = await userProvider.Post(
                        (long)sessionState["UserId"],
                        context.Request.Form["post"]);

                    context.Response.Write(string.Format("New post with id {0} created", postId));
                }
            }
            else if (context.Request.Path.Contains("follow"))
            {
                UserProvider userProvider = new UserProvider();

                context.Response.ContentType = "text/plain";
                HttpSessionState sessionState = context.Session;
                if (context.Session == null || sessionState["UserId"] == null)
                {
                    context.Response.Write("No user logged in on this browser");
                }
                else
                {
                    // Follow the target
                    var followed = await userProvider.FollowUser(
                        (long)sessionState["UserId"],
                        context.Request.Form["target"]);

                    if (followed)
                    {
                        context.Response.Write(string.Format("successfully followed user: {0}", context.Request.Form["target"]));
                    }
                    else
                    {
                        context.Response.Write("Follow request failed");
                    }
                }
            }
            else if (context.Request.Path.Contains("getFeed"))
            {
                UserProvider userProvider = new UserProvider();

                context.Response.ContentType = "text/plain";
                HttpSessionState sessionState = context.Session;
                if (context.Session == null || sessionState["UserId"] == null)
                {
                    context.Response.Write("No user logged in on this browser");
                }
                else
                {
                    // See if the user is available
                    var posts = await userProvider.GetPostsFromFollows(
                        (long)sessionState["UserId"]);

                    StringBuilder postResponse = new StringBuilder();

                    foreach (Post post in posts)
                    {
                        postResponse.AppendFormat("{0} says: {1}<br>", post.PosterName, post.PostBody);
                    }

                    context.Response.Write(postResponse.ToString());
                }
            }
            else if (context.Request.Path.Contains("getPublicPosts"))
            {
                UserProvider userProvider = new UserProvider();

                context.Response.ContentType = "text/plain";

                // See if the user is available
                var posts = await userProvider.GetPublicTimeline();

                StringBuilder postResponse = new StringBuilder();

                foreach (Post post in posts)
                {
                    postResponse.AppendFormat("{0} says: {1}<br>", post.PosterName, post.PostBody);
                }

                context.Response.Write(postResponse.ToString());
            }
            else if (context.Request.Path.Contains("test"))
            {
                UserProvider userProvider = new UserProvider();
                context.Response.ContentType = "text/plain";
                HttpSessionState sessionState = context.Session;
                if (context.Session == null || sessionState["UserId"] == null)
                {
                    context.Response.Write("No user logged in on this browser");
                }
                else
                {
                    string username = await userProvider.GetUsername((long)sessionState["UserId"]);
                    context.Response.Write(string.Format("User {0} with id {1} is logged in!", username, sessionState["UserId"]));
                }
            }
            else if(context.Request.Path.Contains("logout"))
            {
                context.Response.ContentType = "text/plain";
                HttpSessionState sessionState = context.Session;
                if (context.Session != null)
                {
                    sessionState.Remove("UserId");
                }
                context.Response.Write("Logged out...");
            }
            else if(context.Request.Path.Contains("clearStore"))
            {
                await Dependencies.Instance.Value.ClearStore();

                context.Response.Write("Done");
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public IHttpHandler GetHttpHandler(RequestContext
            requestContext)
        {
            var page = BuildManager.CreateInstanceFromVirtualPath
                 ("~/TwitOperations.ashx", typeof(IHttpHandler)) as IHttpHandler;
            return page;
        }
    }
}