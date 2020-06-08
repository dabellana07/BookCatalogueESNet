using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookCatalogueESNet.ElasticSearch.Documents;

namespace BookCatalogueESNet.Contracts.ElasticSearch.Services
{
    public interface IBookElasticService
    {
        Task<Book> AddBook(Book book);
        Task RemoveBook(Guid book);
        Task UpdateBook(Book book);
        Task<IEnumerable<Book>> SearchBooks(
            string searchValue,
            string genre = "",
            DateTime? startDate = null,
            DateTime? endDate = null,
            int take = 10);
        Task<Book> GetBook(Guid id);
    }
}