using HtmlAgilityPack;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedCommands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Flow.Launcher.Plugin.CPPreference
{
  public class Main : IPlugin
  {
    private PluginInitContext _context;

    private readonly string query_url = "https://en.cppreference.com/mwiki/index.php?search=";
    private readonly string icon_path = "icon.png";
    private readonly bool _cpp = true;

    //------------------------------------------------------------------------------
    public void Init(PluginInitContext context)
    {
      _context = context;
    }

    //------------------------------------------------------------------------------
    public List<Result> Query(Query query)
    {
      List<Result> results = new List<Result>();
      string[] args = query.RawQuery.Split(' ');
      bool open_page = false;
      string url = query_url;

      if (args.Length >= 2)
      {

        if (args[1].CompareTo("d") == 0)
          open_page = true;
      }
      else
      {
        return results;
      }

      int index = (open_page ? 2 : 1);

      while (index < args.Length)
      {
        url += args[index];
        if (index != args.Length - 1)
          url += '+';
        index++;
      }

      if (open_page)
      {

        var result = new Result
        {
          Title = "search in cppreference.com",
          SubTitle = url,
          IcoPath = icon_path,
          Action = e =>
          {
            url.NewTabInBrowser();
            return true;
          }
        };

        results.Add(result);
      }
      else
      {
        HtmlWeb html = new HtmlWeb();
        HtmlDocument document = html.Load(url);
        string title = document.DocumentNode.FirstChild.NextSibling.NextSibling.FirstChild.NextSibling.FirstChild.NextSibling.InnerText;

        if (!title.Contains("Search results"))
        {
          var result = new Result
          {
            Title = title.Replace(" - cppreference.com", ""),
            SubTitle = url,
            IcoPath = icon_path,
            Action = e =>
            {
              url.NewTabInBrowser();
              return true;
            }
          };

          results.Add(result);
          //return results;
        }

        List<HtmlNode> search_results = GetElementByClass(document.GetElementbyId("mw-content-text"), "mw-search-results");

        if (search_results.Count == 0 || (search_results.Count == 1 && !_cpp))
        {

          var result = new Result
          {
            Title = "no result",
            IcoPath = icon_path,
            Action = e =>
            {
              return false;
            }
          };

          results.Add(result);
        }
        else
        {
          List<HtmlNode> nodes = GetAllSearchResults(search_results[_cpp ? 0 : 1]);

          foreach (HtmlNode node in nodes)
          {
            string query_url = "https://en.cppreference.com" + node.Attributes["href"].Value;
            var result = new Result
            {
              Title = node.InnerText.Replace("&lt;", "<").Replace("&gt;", ">"),
              SubTitle = query_url,
              IcoPath = icon_path,
              Action = e =>
              {
                try
                {
                  query_url.NewTabInBrowser();
                  return true;
                }
                catch (Exception)
                { return false; }
              }
            };

            results.Add(result);
          }

        }
      }

      return results;
    }

    //------------------------------------------------------------------------------
    public static List<HtmlNode> GetAllSearchResults(HtmlNode node)
    {
      List<HtmlNode> nodes = new List<HtmlNode>();
      if (node.Name == "a" && !node.InnerText.Contains("pmr"))
      {
        nodes.Add(node);
      }

      foreach (HtmlNode child in node.ChildNodes)
      {
        nodes.AddRange(GetAllSearchResults(child));
      }

      return nodes;
    }

    //------------------------------------------------------------------------------
    public static List<HtmlNode> GetElementByClass(HtmlNode root, string class_name)
    {
      List<HtmlNode> res = new List<HtmlNode>();
      Queue<HtmlNode> nodes = new Queue<HtmlNode>();
      HtmlNode node = null;
      nodes.Enqueue(root);

      while (nodes.Count != 0)
      {
        node = nodes.Dequeue();
        HtmlNode child = node.FirstChild;

        while (child != null)
        {
          nodes.Enqueue(child);
          child = child.NextSibling;
        }

        HtmlAttribute attribute = node.Attributes["class"];

        if (attribute != null && attribute.Value == class_name)
        {
          res.Add(node);
        }
      }

      return res;
    }
  }
}
