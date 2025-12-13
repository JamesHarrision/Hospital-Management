using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public class Doctor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; } = "BS."; // Ví dụ: BS, ThS, TS
        public string Specialization { get; set; } // Chuyên khoa: Tim mạch, Nha khoa...
        public string ImageSource { get; set; }    // Đường dẫn ảnh
        public string Description { get; set; }    // Mô tả thêm nếu cần

        // Property hỗ trợ hiển thị trên UI (Dropdown/List)
        public string FullName => $"{Title} {Name}";
    }
}
