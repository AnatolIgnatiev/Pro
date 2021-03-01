using System.Windows.Input;

namespace Pro
{
    public class FileItemViewModel
    {
        public string FileName { get; set; }

        public ICommand RemoveItem { get; set; }
    }
}