using System;
using System.Threading;

namespace DevZest.DataVirtualizationDemo
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public Person(int id)
        {
            Id = id;
            FirstName = String.Format("FirstName{0}", id);
            LastName = String.Format("LastName{0}", id);
        }
    }
}
