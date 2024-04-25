using Ardalis.Specification.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Data;

public class MoongladeRepository<T>(BlogDbContext dbContext) : RepositoryBase<T>(dbContext)
    where T : class
{

}