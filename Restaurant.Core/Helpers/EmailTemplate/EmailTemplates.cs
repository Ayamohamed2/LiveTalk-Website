using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Core.Helpers.EmailTemplate
{
    public class EmailTemplates
    {
        private static string BaseTemplate(string accentColor, string icon, string title, string bodyContent)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8""/>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <title>{title}</title>
</head>
<body style=""margin:0;padding:0;background:#0f0f0f;font-family:'Georgia',serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#0f0f0f;padding:40px 20px;"">
    <tr><td align=""center"">
      <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;width:100%;"">
 
        <!-- BRAND -->
        <tr><td align=""center"" style=""padding-bottom:28px;"">
          <span style=""font-family:'Georgia',serif;font-size:20px;font-weight:bold;
                        letter-spacing:6px;color:#ffffff;text-transform:uppercase;"">
            RESTAURANT
          </span>
        </td></tr>
 
        <!-- CARD -->
        <tr><td style=""background:#1a1a1a;border-radius:16px;overflow:hidden;border:1px solid #2a2a2a;"">
 
          <!-- ACCENT BAR -->
          <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
            <tr><td style=""height:4px;background:linear-gradient(90deg,{accentColor},{accentColor}99);""></td></tr>
          </table>
 
          <!-- ICON -->
          <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
            <tr><td align=""center"" style=""padding:40px 40px 20px;"">
              <div style=""width:72px;height:72px;border-radius:50%;
                          background:{accentColor}15;border:1.5px solid {accentColor}40;
                          text-align:center;line-height:72px;font-size:30px;"">
                {icon}
              </div>
            </td></tr>
          </table>
 
          <!-- BODY -->
          <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
            <tr><td style=""padding:20px 40px 44px;text-align:center;"">
              {bodyContent}
            </td></tr>
          </table>
 
        </td></tr>
 
        <!-- FOOTER -->
        <tr><td align=""center"" style=""padding-top:24px;"">
          <p style=""margin:0;font-size:12px;color:#444;letter-spacing:1px;"">
            © 2025 RESTAURANT · All rights reserved
          </p>
          <p style=""margin:6px 0 0;font-size:11px;color:#333;"">
            If you didn't request this, you can safely ignore this email.
          </p>
        </td></tr>
 
      </table>
    </td></tr>
  </table>
</body>
</html>";
        }

        // ── 1. CONFIRM EMAIL ──────────────────────────────────────
        public static string ConfirmEmail(string confirmationLink)
        {
            string body = $@"
<h1 style=""margin:0 0 12px;font-size:28px;font-weight:normal;color:#fff;letter-spacing:-0.5px;"">
  Confirm your email
</h1>
<p style=""margin:0 0 32px;font-size:15px;color:#888;line-height:1.7;"">
  Welcome aboard! One last step — verify your email address to activate your account.
</p>
<a href=""{confirmationLink}""
   style=""display:inline-block;padding:14px 40px;background:#D4A853;color:#0f0f0f;
          text-decoration:none;border-radius:8px;font-size:13px;font-weight:bold;
          letter-spacing:2px;text-transform:uppercase;"">
  Confirm Email
</a>
<p style=""margin:24px 0 0;font-size:12px;color:#444;"">
  Link expires in <span style=""color:#D4A853;"">24 hours</span>
</p>";
            return BaseTemplate("#D4A853", "✉", "Confirm Your Email", body);
        }

        // ── 2. OTP CODE ───────────────────────────────────────────
        public static string OtpCode(string code)
        {
            // Build one box per digit
            var boxes = string.Concat(code.Select(c => $@"
<td style=""padding:0 4px;"">
  <div style=""width:52px;height:64px;background:#111;border:1px solid #2a2a2a;
               border-radius:8px;line-height:64px;text-align:center;
               font-size:28px;font-weight:bold;color:#fff;
               font-family:'Courier New',monospace;"">
    {c}
  </div>
</td>"));

            string body = $@"
<h1 style=""margin:0 0 12px;font-size:28px;font-weight:normal;color:#fff;letter-spacing:-0.5px;"">
  Your verification code
</h1>
<p style=""margin:0 0 28px;font-size:15px;color:#888;line-height:1.7;"">
  Use this code to complete your sign-in. Valid for
  <span style=""color:#4ECDC4;"">10 minutes</span>.
</p>
<table align=""center"" cellpadding=""0"" cellspacing=""0"" style=""margin:0 auto 28px;"">
  <tr>{boxes}</tr>
</table>
<p style=""font-size:13px;color:#555;"">
  Never share this code — our team will <strong style=""color:#888;"">never</strong> ask for it.
</p>";
            return BaseTemplate("#4ECDC4", "🔐", "Your OTP Code", body);
        }

        // ── 3. RESET PASSWORD ─────────────────────────────────────
        public static string ResetPassword(string resetLink)
        {
            string body = $@"
<h1 style=""margin:0 0 12px;font-size:28px;font-weight:normal;color:#fff;letter-spacing:-0.5px;"">
  Reset your password
</h1>
<p style=""margin:0 0 32px;font-size:15px;color:#888;line-height:1.7;"">
  We received a request to reset the password for your account.
  Click below to choose a new password.
</p>
<a href=""{resetLink}""
   style=""display:inline-block;padding:14px 40px;background:#E05C5C;color:#fff;
          text-decoration:none;border-radius:8px;font-size:13px;font-weight:bold;
          letter-spacing:2px;text-transform:uppercase;"">
  Reset Password
</a>
<p style=""margin:24px 0 0;font-size:12px;color:#444;"">
  Link expires in <span style=""color:#E05C5C;"">1 hour</span>
</p>";
            return BaseTemplate("#E05C5C", "🔑", "Reset Your Password", body);
        }
    }
}
