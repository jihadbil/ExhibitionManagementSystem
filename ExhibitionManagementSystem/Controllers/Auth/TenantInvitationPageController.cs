using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ExhibitionManagementSystem.Controllers.Auth;

[ApiController]
public sealed class TenantInvitationPageController : ControllerBase
{
    [HttpGet("/accept-invitation")]
    [AllowAnonymous]
    [EnableRateLimiting("authentication")]
    public ContentResult Index()
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        Response.Headers.ContentSecurityPolicy =
            $"default-src 'none'; style-src 'nonce-{nonce}'; script-src 'nonce-{nonce}'; " +
            "connect-src 'self'; img-src 'none'; font-src 'none'; base-uri 'none'; " +
            "form-action 'none'; frame-ancestors 'none'";
        Response.Headers.CacheControl = "no-store";

        return Content(PageHtml.Replace("{{NONCE}}", nonce, StringComparison.Ordinal),
            "text/html; charset=utf-8");
    }

    private const string PageHtml = """
        <!doctype html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>قبول دعوة الإدارة</title>
          <style nonce="{{NONCE}}">
            :root{font-family:Tahoma,Arial,sans-serif;color:#172033;background:#f4f6fa}
            body{margin:0;min-height:100vh;display:grid;place-items:center;padding:20px}
            main{width:min(430px,100%);background:#fff;padding:30px;border-radius:16px;box-shadow:0 12px 36px #17203320}
            h1{font-size:1.5rem;margin:0 0 10px}p{line-height:1.7;color:#526078}
            label{display:block;margin:16px 0 6px;font-weight:700}input{box-sizing:border-box;width:100%;padding:12px;border:1px solid #ccd3df;border-radius:8px;font:inherit}
            button{width:100%;margin-top:22px;padding:13px;border:0;border-radius:8px;background:#165dff;color:white;font:inherit;font-weight:700;cursor:pointer}
            button:disabled{opacity:.6;cursor:wait}.message{min-height:24px;margin-top:16px}.error{color:#b42318}.success{color:#067647}
          </style>
        </head>
        <body>
          <main>
            <h1>إنشاء حساب مدير المستأجر</h1>
            <p>عيّن كلمة مرور قوية لإكمال قبول الدعوة. الرابط أحادي الاستخدام.</p>
            <form id="invitation-form">
              <label for="password">كلمة المرور</label>
              <input id="password" type="password" minlength="8" autocomplete="new-password" required>
              <label for="confirmation">تأكيد كلمة المرور</label>
              <input id="confirmation" type="password" minlength="8" autocomplete="new-password" required>
              <button id="submit" type="submit">قبول الدعوة</button>
            </form>
            <div id="message" class="message" role="status" aria-live="polite"></div>
          </main>
          <script nonce="{{NONCE}}">
            const params=new URLSearchParams(location.search),email=params.get('email'),token=params.get('token');
            const form=document.getElementById('invitation-form'),message=document.getElementById('message'),button=document.getElementById('submit');
            if(!email||!token){form.hidden=true;message.className='message error';message.textContent='رابط الدعوة غير مكتمل.';}
            form.addEventListener('submit',async event=>{
              event.preventDefault();message.textContent='';
              const password=document.getElementById('password').value,confirmPassword=document.getElementById('confirmation').value;
              if(password!==confirmPassword){message.className='message error';message.textContent='كلمتا المرور غير متطابقتين.';return;}
              button.disabled=true;
              try{
                const response=await fetch('/api/tenants/invitations/accept',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,token,password,confirmPassword})});
                if(!response.ok){const problem=await response.json().catch(()=>({}));throw new Error(problem.errorMessage||'تعذر قبول الدعوة.');}
                history.replaceState({},'',location.pathname);form.hidden=true;message.className='message success';message.textContent='تم إنشاء الحساب بنجاح. يمكنك الآن تسجيل الدخول إلى التطبيق.';
              }catch(error){message.className='message error';message.textContent=error.message||'تعذر قبول الدعوة.';}finally{button.disabled=false;}
            });
          </script>
        </body>
        </html>
        """;
}
