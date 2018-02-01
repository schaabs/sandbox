using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sandbox.entity
{
    class SandboxDb : DbContext
    {
        public SandboxDb() : base("DefaultConnection")
        {
            Database.SetInitializer<SandboxDb>(new DropCreateDatabaseAlways<SandboxDb>());
        }

        public DbSet<Foo> Foos { get; set; }
        
    }

    class Foo
    {

        [Key]
        public string FooId { get; set; }

        [StringLength(100)]
        public string Text { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}
