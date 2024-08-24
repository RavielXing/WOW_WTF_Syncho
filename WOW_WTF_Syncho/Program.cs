using System.Windows.Threading;

namespace ConsoleApp1
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Window1 window1 = new Window1();
            window1.ShowDialog();
        }
    }
}
