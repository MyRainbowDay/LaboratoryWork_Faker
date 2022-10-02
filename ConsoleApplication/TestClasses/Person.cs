using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication.TestClasses
{
    public class Person
    {
        public Person(string name, string surname, int age, bool isHasDog)
        {
            Name = name;
            Surname = surname;
            Age = age;
            IsHasDog = isHasDog;
        }

        public int TimesHeWasFired { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }
        public int Age { get; set; }
        public bool IsHasDog { get; set; }

    }
}
