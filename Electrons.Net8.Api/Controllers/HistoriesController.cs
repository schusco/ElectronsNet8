using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Electrons.Net8.Api.Models;
using Electrons.Net8.Api;

[Route("api/[controller]")]
[ApiController]
public class HistoriesController : ControllerBase
{
    private readonly ElectronsDbContext _context;
    public HistoriesController(ElectronsDbContext context)
    {
        _context = context;
    }

    // GET: api/History
    [HttpGet]
    public async Task<ActionResult<IEnumerable<History>>> GetHistory()
    {
        return await _context.History.ToListAsync();
    }

    // GET: api/History/5
    [HttpGet("{id}")]
    public async Task<ActionResult<History>> GetHistory(int id)
    {
        var history = await _context.History.FindAsync(id);

        if (history == null)
        {
            return NotFound();
        }

        return history;
    }

    // PUT: api/History/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutHistory(int? id, History history)
    {
        if (id != history.Id)
        {
            return BadRequest();
        }

        _context.Entry(history).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!HistoryExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/History
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<History>> PostHistory(History history)
    {
        _context.History.Add(history);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetHistory", new { id = history.Id }, history);
    }

    // DELETE: api/History/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHistory(int? id)
    {
        var history = await _context.History.FindAsync(id);
        if (history == null)
        {
            return NotFound();
        }

        _context.History.Remove(history);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool HistoryExists(int? id)
    {
        return _context.History.Any(e => e.Id == id);
    }
}
