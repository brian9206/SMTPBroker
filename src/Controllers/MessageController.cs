using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMTPBroker.Persistence;

namespace SMTPBroker.Controllers;

[Route("[controller]")]
public class MessageController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public MessageController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [Route("")]
    public async Task<IActionResult> List()
    {
        var messages = await _context.Messages.AsNoTracking()
            .OrderByDescending(msg => msg.DatedAt)
            .ToArrayAsync();
        
        return View(messages);
    }

    [Route("{messageId}")]
    public async Task<IActionResult> View(Guid messageId)
    {
        var message = await _context.Messages.SingleOrDefaultAsync(message => message.Id == messageId);
        if (message == null)
            return RedirectToAction("List");

        return View(message);
    }
    
    [Route("{messageId}/attachment/{attachmentId}")]
    public async Task<IActionResult> DownloadAttachment(Guid messageId, Guid attachmentId)
    {
        var message = await _context.Messages.SingleOrDefaultAsync(message => message.Id == messageId);
        if (message == null)
            return RedirectToAction("List");

        var attachment = message.Attachments.SingleOrDefault(attachment => attachment.Id == attachmentId);
        if (attachment == null)
            return RedirectToAction("List");

        var fileName = Path.Combine(_configuration.GetValue<string>("DataDir"), "attachment_" + attachment.Id + ".bin");
        if (!System.IO.File.Exists(fileName))
            return RedirectToAction("List");
        
        var file = System.IO.File.OpenRead(fileName);
        return File(file, attachment.MimeType, attachment.FileName);
    }
}