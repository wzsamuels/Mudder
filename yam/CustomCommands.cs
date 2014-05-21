using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam
{
    public class CustomCommands
    {
        public RoutedUICommand MyCustomCommand 
                               = new RoutedUICommand("My custom command", 
                                                     "MyCustomCommand",
                                                     typeof(CustomCommands));
    }
}
