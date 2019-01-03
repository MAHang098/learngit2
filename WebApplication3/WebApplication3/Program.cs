using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //DBHelper.ConnectionString = "Data Source=192.16.4.22;Initial Catalog=DB_KBT;User ID=sa;Password=kbt201182126631KBT";
            DBHelper.ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=test";
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
