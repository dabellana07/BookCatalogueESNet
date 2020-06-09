using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using BookCatalogueESNet.Contracts.ElasticSearch.Services;
using BookCatalogueESNet.ElasticSearch.Documents;
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.IndicesApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BookCatalogueESNet.ElasticSearch
{
    public class BookService : IBookElasticService
    {
        private readonly IElasticLowLevelClient _client;
        private readonly ILogger<BookService> _logger;
        private const string IndexName = "books";

        public BookService(IElasticLowLevelClient client, ILogger<BookService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<IEnumerable<Book>> SearchBooks(
            string searchValue = "",
            string genre = "",
            DateTime? startDate = null,
            DateTime? endDate = null,
            int take = 10)
        {
            var searchResponseData = ConstructSearchDate(
                searchValue?.ToLower(), genre?.ToLower(), startDate, endDate, take);
            var searchResponse = _client.Search<StringResponse>(
                IndexName, PostData.Serializable(searchResponseData));

            if (!searchResponse.Success)
            {
                throw searchResponse.OriginalException;
            }

            var data = (JObject) JsonConvert.DeserializeObject(searchResponse.Body);
            var hits = data["hits"]["hits"] as JArray;
            var books = new List<Book>();
            foreach (var hit in hits)
            {
                var source = hit["_source"].ToString();
                var book = JsonSerializer.Deserialize<Book>(source);
                books.Add(book);
            }

            return books;
        }

        public async Task<Book> GetBook(Guid id)
        {
            var response = await _client.GetAsync<StringResponse>(IndexName, id.ToString());
            if (response.Success)
            {
                var data = (JObject) JsonConvert.DeserializeObject(response.Body);
                var source = data.GetValue("_source").ToString();
                var book = JsonSerializer.Deserialize<Book>(source);
                return book;
            }

            throw response.OriginalException;
        }

        public async Task<Book> AddBook(Book book)
        {
            var id = Guid.NewGuid();

            book.Id = id;

            var indexResponse = await _client.IndexAsync<StringResponse>(
                IndexName, id.ToString(), PostData.Serializable(book));

            if (indexResponse.Success)
            {
                return book;
            }

            throw indexResponse.OriginalException;
        }

        public async Task RemoveBook(Guid id)
        {
            var response = await _client.DeleteAsync<StringResponse>(IndexName, id.ToString());
            if (!response.Success)
            {
                throw response.OriginalException;
            }
        }

        public async Task UpdateBook(Book book)
        {
            var response = await _client.UpdateAsync<StringResponse>(
                IndexName,
                book.Id.ToString(),
                PostData.Serializable(new
                {
                    doc = book
                }));

            if (!response.Success)
            {
                throw response.OriginalException;
            }
        }

        private dynamic ConstructSearchDate(
            string searchValue, string genre, DateTime? startDate, DateTime? endDate, int take)
        {
            // Title, Genre, PublishDate
            if (!string.IsNullOrEmpty(searchValue)
                && (startDate.HasValue || endDate.HasValue)
                && !string.IsNullOrEmpty(genre))
            {
                return new
                {
                    size = take,
                    query = new
                    {
                        @bool = new
                        {
                            must = new
                            {
                                match = new
                                {
                                    Title = searchValue
                                }
                            },
                            filter = new
                            {
                                range = new
                                {
                                    PublishDate = GetRangeFilter(startDate, endDate)
                                },
                                term = new
                                {
                                    Genre = genre
                                }
                            }
                        }
                    }
                };
            }

            // PublishDate and Genre
            if (string.IsNullOrEmpty(searchValue)
                && (startDate.HasValue || endDate.HasValue)
                && !string.IsNullOrEmpty(genre))
            {
                return new
                {
                    query = new
                    {
                        @bool = new
                        {
                            filter = new
                            {
                                range = new
                                {
                                    PublishDate = GetRangeFilter(startDate, endDate)
                                },
                                term = new
                                {
                                    Genre = genre
                                }
                            }
                        }
                    }
                };
            }

            // Title and Genre
            if (!string.IsNullOrEmpty(searchValue)
                && (!startDate.HasValue && !endDate.HasValue)
                && !string.IsNullOrEmpty(genre))
            {
                return new
                {
                    query = new
                    {
                        @bool = new
                        {
                            must = new
                            {
                                match = new
                                {
                                    Title = searchValue
                                }
                            },
                            filter = new
                            {
                                term = new
                                {
                                    Genre = genre
                                }
                            }
                        }
                    }
                };
            }

            // Title and PublishDate
            if (!string.IsNullOrEmpty(searchValue)
                && (startDate.HasValue || endDate.HasValue)
                && string.IsNullOrEmpty(genre))
            {
                return new
                {
                    query = new
                    {
                        @bool = new
                        {
                            must = new
                            {
                                match = new
                                {
                                    Title = searchValue
                                }
                            },
                            filter = new
                            {
                                range = new
                                {
                                    PublishDate = GetRangeFilter(startDate, endDate)
                                },
                            }
                        }
                    }
                };
            }

            // Publish Date
            if ((startDate.HasValue || endDate.HasValue)
                && string.IsNullOrEmpty(genre)
                && string.IsNullOrEmpty(searchValue))
            {
                return new
                {
                    query = new
                    {
                        @bool = new
                        {
                            filter = new
                            {
                                range = new
                                {
                                    PublishDate = GetRangeFilter(startDate, endDate)
                                }
                            }
                        }
                    }
                };
            }

            // Title
            if ((!startDate.HasValue && !endDate.HasValue)
                && string.IsNullOrEmpty(genre)
                && !string.IsNullOrEmpty(searchValue))
            {
                return new
                {
                    query = new
                    {
                        match = new
                        {
                            Title = new
                            {
                                query = searchValue
                            }
                        }
                    }
                };
            }

            // Genre
            if ((!startDate.HasValue && !endDate.HasValue)
                && !string.IsNullOrEmpty(genre)
                && string.IsNullOrEmpty(searchValue))
            {
                return new
                {
                    query = new
                    {
                        @bool = new
                        {
                            filter = new
                            {
                                term = new
                                {
                                    Genre = genre
                                }
                            }
                        }
                    }
                };
            }

            return new { };
        }

        private dynamic GetRangeFilter(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return new
                {
                    gte = startDate.Value.ToUniversalTime(),
                    lte = endDate.Value.ToUniversalTime()
                };
            }

            if (startDate.HasValue)
            {
                return new
                {
                    gte = startDate.Value.ToUniversalTime(),
                };
            }

            if (endDate.HasValue)
            {
                return new
                {
                    lte = endDate.Value.ToUniversalTime()
                };
            }

            return new { };
        }

        public void InitClient()
        {
            var postData = new
            {
                settings = new
                {
                    analysis = new
                    {
                        filter = new
                        {
                            custom_latin_transformer = new
                            {
                                type = "icu_transform",
                                id = "Any-Latin; NFD; [:Nonspacing Mark:] Remove; NFC"
                            }
                        },
                        analyzer = new
                        {
                            latin = new
                            {
                                tokenizer = "keyword",
                                filter = new[] {"custom_latin_transformer"}
                            }
                        }
                    }
                }
            };
            var param = new CreateIndexRequestParameters
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            var response = _client.Indices.Create<StringResponse>(
                IndexName, PostData.Serializable(postData), param);

            if (!response.Success)
            {
                _logger.LogError("Index Create Not Successful: " + response.OriginalException.ToString());
            }
            else
            {
                _logger.LogInformation("Index Create Successful");
            }
        }
    }
}