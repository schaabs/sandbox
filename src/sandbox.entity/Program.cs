using sandbox.tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Migrations;

namespace sandbox.entity
{
    class Program : Sandbox
    {
        static void Main(string[] args)
        {
            sandbox(Run);
        }

        static void Run()
        {
            string fooId = Guid.NewGuid().ToString("N");

            using (SandboxDb sandboxDb = new SandboxDb())
            {
                var foo = new Foo() { FooId = fooId, Text = "Original Foo" };

                sandboxDb.Foos.Add(foo);

                sandboxDb.SaveChanges();
            }


            using (SandboxDb sandboxDb = new SandboxDb())
            {
                var foo = new Foo() { FooId = fooId, Text = "Updated Foo" };

                sandboxDb.Foos.AddOrUpdate(foo);

                sandboxDb.SaveChanges();
            }

            using (SandboxDb sandboxDb = new SandboxDb())
            {
                var foo = sandboxDb.Foos.Find(fooId);

                print(foo.Text);
            }

        }
    }
}
