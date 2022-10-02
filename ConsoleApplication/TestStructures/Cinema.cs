using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication.TestStructures
{
    public struct Cinema
    {
        public int NumberOfSeats { get; set; }
        public int NumberOfFloors { get; set; }

        public Cinema(int numberOfSeats, int numberOfFloors)
        {
            NumberOfSeats = numberOfSeats;
            NumberOfFloors = numberOfFloors;
        }
    }
}
