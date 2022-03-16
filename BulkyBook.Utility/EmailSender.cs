﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;


namespace BulkyBook.Utility
{
    public class EmailSender:IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailToSend = new MimeMessage();
            emailToSend.From.Add(MailboxAddress.Parse("tutaj@dodajemail.com"));//email to configure
            emailToSend.To.Add(MailboxAddress.Parse(email));
            emailToSend.Subject = subject;
            emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html){Text = htmlMessage};

            //send email to client
            using (var emailClient = new SmtpClient())
            {
                // add smtp to  correct email address 
                emailClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                //add email and password BEFORE SEND CORRECT EMAIL NEED TO CONFIGURE EMAIL SENDER SITE TO ADD ACCESS TO SEND AUTO EMAIL 
                emailClient.Authenticate("tutaj@dodajemail.com", "tutajDodajHasło");
                emailClient.Send(emailToSend);
                emailClient.Disconnect(true);
            }
            return Task.CompletedTask;
        }
    }
}
