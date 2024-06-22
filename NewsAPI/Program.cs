using NewsAPI.Models;
using NewsAPI.Constants;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Text;

namespace NewsAPI
{
    class Program
    {
        static Subject<List<Article>> articles_s = new();
        static Subject<ProcessedArticle[]> processedArticles_s = new();
        static Subject<ProcessedArticle[]> sortedProcessedArticles_s = new();
        static void Main()
        {
            var newsApiClient = new NewsApiClient("5ccf7e08f76a4091818f8e9bd27c8dc7");

            var keyword_s = new Subject<string>();
            var country_s = new Subject<string>();
            var keywordAndCountry_s = keyword_s.Zip(country_s);

            Console.WriteLine("Main thread: " + System.Environment.CurrentManagedThreadId);

            keywordAndCountry_s.Subscribe(async (tuple) =>
            {

                Console.WriteLine("Got keyword and country on thread: " + System.Environment.CurrentManagedThreadId);
                if (Enum.TryParse(tuple.Second, out Countries country))
                {
                    try
                    {
                        var articlesResponse = await newsApiClient.GetTopHeadlinesAsync(new TopHeadlinesRequest
                        {
                            Q = tuple.First,
                            Country = country
                        });
                        // Console.WriteLine("Status: " + articlesResponse.Status);
                        // Console.WriteLine("Total results: " + articlesResponse.TotalResults);
                        // Console.WriteLine("Error: " + articlesResponse.Error);
                        articles_s.OnNext(articlesResponse.Articles);
                    }
                    catch (Exception err) { Console.WriteLine("ERR: " + err); }
                }
                else
                {
                    Console.WriteLine("Invalid country value");
                }
            });

            articles_s.Subscribe((articles) =>
            {
                Console.WriteLine("Got articles on thread: " + System.Environment.CurrentManagedThreadId);
                ProcessArticles(articles);
            });

            processedArticles_s.Subscribe((processedArticles) =>
            {
                Console.WriteLine("Got processed articles on thread: " + System.Environment.CurrentManagedThreadId);
                Array.Sort(processedArticles);
                sortedProcessedArticles_s.OnNext(processedArticles);
            });

            sortedProcessedArticles_s.Subscribe((processedArticles) =>
            {
                Console.WriteLine("Got sorted processed articles on thread: " + System.Environment.CurrentManagedThreadId);
                PrintProcessedArticles(processedArticles);
            });

            for (int i = 0; i < 10; i++)
            {
                keyword_s.OnNext("trump");
                country_s.OnNext("US");
            }
            keyword_s.OnNext("trump");
            keyword_s.OnNext("trump");
            country_s.OnNext("US");
            country_s.OnNext("US");

            while (true)
            {
                Console.Write("Keyword: ");
                var keyword = Console.ReadLine();
                if (keyword != null && !string.IsNullOrWhiteSpace(keyword))
                {
                    keyword_s.OnNext(keyword);
                }

                Console.Write("Country: ");
                var country = Console.ReadLine();
                if (country != null && !string.IsNullOrWhiteSpace(country))
                {
                    country_s.OnNext(country);
                }
            }
        }
        static void ProcessArticles(IEnumerable<Article> articles)
        {
            // Console.WriteLine($"About to process: {articles.Count()} articles");
            var processedArticles = new ProcessedArticle[articles.Count()];
            Parallel.ForEach(articles, (article, state, i) =>
            {
                processedArticles[i] = ProcessArticle(article);
            });

            processedArticles_s.OnNext(processedArticles);
        }
        static ProcessedArticle ProcessArticle(Article article)
        {
            if (string.IsNullOrWhiteSpace(article.Description))
            {
                return new ProcessedArticle
                {
                    Article = article,
                    UncapitalizedWordsCount = -1,
                    UniqueWordsCount = -1,
                };
            }
            var allWords = article.Description.Split(' ');
            int uncapitalizedWordsCount = allWords.Where(w => char.IsLower(w[0])).Count();
            int uniqueWordsCount = allWords.GroupBy(w => w).Where(g => g.Count() == 1).Count();

            return new ProcessedArticle
            {
                Article = article,
                UncapitalizedWordsCount = uncapitalizedWordsCount,
                UniqueWordsCount = uniqueWordsCount,
            };
        }
        static void PrintProcessedArticles(ProcessedArticle[] processedArticles)
        {
            StringBuilder sb = new();
            foreach (var processedArticle in processedArticles)
            {
                sb.Append("Title:\n");
                sb.Append(processedArticle.Article.Title);
                sb.Append("\n");

                sb.Append("Description:\n");
                sb.Append(processedArticle.Article.Description);
                sb.Append("\n");

                sb.Append("Uncapitalized words: ");
                sb.Append(processedArticle.UncapitalizedWordsCount);
                sb.Append(", Unique words: ");
                sb.Append(processedArticle.UniqueWordsCount);
                sb.Append("\n");
                sb.Append("\n");

            }
            sb.Append("\n");
            sb.Append("Articles Count: ");
            sb.Append(processedArticles.Length);
            sb.Append("\n");
            sb.Append("\n");

            Console.WriteLine(sb);
        }
    }
    struct ProcessedArticle : IComparable<ProcessedArticle>
    {
        public Article Article;
        public int UncapitalizedWordsCount;
        public int UniqueWordsCount;

        public int CompareTo(ProcessedArticle other)
        {
            return UniqueWordsCount - other.UniqueWordsCount;
        }
    }
}
