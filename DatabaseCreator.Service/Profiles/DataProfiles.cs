using AutoMapper;
using DatabaseCreator.Domain.Dto;
using DatabaseCreator.Domain.Models;

namespace DatabaseCreator.Service.Profiles
{
    public class DataProfiles : Profile
    {
        public DataProfiles()
        {
            CreateMap<DbInfodto, DbInfo>(MemberList.None);
        }
    }
}
