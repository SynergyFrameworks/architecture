using System;
using System.Collections.Generic;
using System.Text;

namespace MML.Api.Application.Interfaces
{
    public interface IAuthenticatedUserService
    {
        string UserId { get; }
    }
}
