using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HosipitalManager.Helpers
{
    public class ValidationHelper
    {
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            // Regex này chấp nhận chữ cái unicode (bao gồm tiếng Việt) và khoảng trắng
            // \p{L}: Bất kỳ chữ cái nào (Latin, Việt, Hoa...)
            // \s: Khoảng trắng
            string pattern = @"^[\p{L}\s]+$";

            return Regex.IsMatch(name.Trim(), pattern);
        }
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            // Regex này chấp nhận số điện thoại Việt Nam với định dạng chuẩn
            // Bắt đầu bằng số 0, theo sau là 9 hoặc 10 chữ số
            string pattern = @"^0\d{9}$";
            return Regex.IsMatch(phoneNumber.Trim(), pattern);
        }
        public static (bool IsValid, string Message) IsValidDateOfBirth(DateTime dob)
        {
            if (dob.Date > DateTime.Now.Date)
            {
                return (false, "Ngày sinh không được lớn hơn ngày hiện tại.");
            }

            if (dob.Year < DateTime.Now.Year - 130)
            {
                return (false, "Năm sinh không hợp lệ (Quá cao tuổi).");
            }

            return (true, string.Empty);
        }

        public static bool IsValidLength(string text, int minLength)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Trim().Length >= minLength;
        }
    }
}
