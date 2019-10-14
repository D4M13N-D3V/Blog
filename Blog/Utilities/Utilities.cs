using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace Blog.Utilities
{
    public static class ImageUploadValidator
    {
        public static bool IsWebFriendlyImage(HttpPostedFileBase file)
        {
            if (file == null) return false;

            //if (file.ContentLength > 2 * 1024 * 1024 || file.ContentLength < 1024) return false;

            try { using (var img = Image.FromStream(file.InputStream)) { return ImageFormat.Jpeg.Equals(img.RawFormat) || ImageFormat.Png.Equals(img.RawFormat) || ImageFormat.Gif.Equals(img.RawFormat); } } catch { return false; }

        }
    }

    public class EmailInformation
    {
        public List<string> Reciepents = new List<string>();
        public string Title = "";
        public string Body = "";
    }

    public class Utilities
    {
        public static string GetBaseUrl()
        {
            var request = HttpContext.Current.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;

            if (appUrl != "/")
                appUrl = "/" + appUrl;

            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);

            return baseUrl;
        }

        public static bool SendEmail(EmailInformation info)
        {
            using (var msg = new MailMessage())
            {
                foreach(var email in info.Reciepents)
                {
                    msg.To.Add(new MailAddress(email));
                }
                msg.From = new MailAddress(ConfigurationManager.AppSettings.Get("EmailUsername"));
                msg.Subject = info.Title;
                msg.Body = info.Body.ToString();
                msg.IsBodyHtml = true;

                var client = new SmtpClient
                {
                    Host = ConfigurationManager.AppSettings.Get("SMTPServer"),
                    Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings.Get("EmailUsername"), ConfigurationManager.AppSettings.Get("EmailPassword")),
                    Port = Int32.Parse(ConfigurationManager.AppSettings.Get("SMTPPort")),
                    EnableSsl = true
                };

                // You can use Port 25 if 587 is blocked 
                try
                {
                    client.Send(msg);
                    return true;
                }
                catch (SmtpException smtpEx)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Produces optional, URL-friendly version of a title, "like-this-one". 
        /// hand-tuned for speed, reflects performance refactoring contributed
        /// by John Gietzen (user otac0n) 
        /// </summary>
        public static string URLFriendly(string title)
        {
            if (title == null) return "";
            const int maxlen = 80;
            int len = title.Length;
            bool prevdash = false;
            var sb = new StringBuilder(len);
            char c;
            for (int i = 0; i < len; i++)
            {
                c = title[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                    prevdash = false;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    // tricky way to convert to lowercase
                    sb.Append((char)(c | 32));
                    prevdash = false;
                }
                else if (c == ' ' || c == ',' || c == '.' || c == '/' ||
                c == '\\' || c == '-' || c == '_' || c == '=')
                {
                    if (!prevdash && sb.Length > 0)
                    {
                        sb.Append('-');
                        prevdash = true;
                    }
                }
                else if (c == '#')
                {
                    if (i > 0)
                        if (title[i - 1] == 'C' || title[i - 1] == 'F')
                            sb.Append("-sharp");
                }
                else if (c == '+')
                {
                    sb.Append("-plus");
                }
                else if ((int)c >= 128)
                {
                    int prevlen = sb.Length;
                    sb.Append(RemapInternationalCharToAscii(c));
                    if (prevlen != sb.Length) prevdash = false;
                }
                if (sb.Length == maxlen) break;
            }
            if (prevdash)
                return sb.ToString().Substring(0, sb.Length - 1);
            else
                return sb.ToString();
        }
        public static string RemapInternationalCharToAscii(char c)
        {
            string s = c.ToString().ToLowerInvariant();
            if ("àåáâäãåą".Contains(s))
            {
                return "a";
            }
            else if ("èéêëę".Contains(s))
            {
                return "e";
            }
            else if ("ìíîïı".Contains(s))
            {
                return "i";
            }
            else if ("òóôõöøőð".Contains(s))
            {
                return "o";
            }
            else if ("ùúûüŭů".Contains(s))
            {
                return "u";
            }
            else if ("çćčĉ".Contains(s))
            {
                return "c";
            }
            else if ("żźž".Contains(s))
            {
                return "z";
            }
            else if ("śşšŝ".Contains(s))
            {
                return "s";
            }
            else if ("ñń".Contains(s))
            {
                return "n";
            }
            else if ("ýÿ".Contains(s))
            {
                return "y";
            }
            else if ("ğĝ".Contains(s))
            {
                return "g";
            }
            else if (c == 'ř')
            {
                return "r";
            }
            else if (c == 'ł')
            {
                return "l";
            }
            else if (c == 'đ')
            {
                return "d";
            }
            else if (c == 'ß')
            {
                return "ss";
            }
            else if (c == 'Þ')
            {
                return "th";
            }
            else if (c == 'ĥ')
            {
                return "h";
            }
            else if (c == 'ĵ')
            {
                return "j";
            }
            else
            {
                return "";
            }
        }

    }

}