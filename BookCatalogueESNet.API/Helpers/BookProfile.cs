using AutoMapper;
using BookCatalogueESNet.API.DTO;
using BookCatalogueESNet.ElasticSearch.Documents;

namespace BookCatalogueESNet.API.Helpers
{
    public class BookProfile : Profile
    {
        public BookProfile()
        {
            CreateMap<Book, BookDTO>();
            CreateMap<BookDTO, Book>();
        }
    }
}
