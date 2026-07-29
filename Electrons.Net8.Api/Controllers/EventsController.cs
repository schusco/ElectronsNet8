using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Electrons.Net8.Api.Models;
using Electrons.Net8.Api;

[Route("api/[controller]")]
[ApiController]
public class EventsController : ControllerBase
{
    private readonly ElectronsDbContext _context;
    public EventsController(ElectronsDbContext context)
    {
        _context = context;
    }

    // GET: api/Events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        return await _context.Events.ToListAsync();
    }

    // GET: api/Events/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvents(int id)
    {
        var events = await _context.Events.FindAsync(id);

        if (events == null)
        {
            return NotFound();
        }

        return events;
    }

    // PUT: api/Events/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEvents(int? id, Event events)
    {
        if (id != events.Id)
        {
            return BadRequest();
        }

        _context.Entry(events).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EventsExists(id))
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

    // POST: api/Events
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Event>> PostEvents(Event events)
    {
        _context.Events.Add(events);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetEvents", new { id = events.Id }, events);
    }

    // DELETE: api/Events/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvents(int? id)
    {
        var events = await _context.Events.FindAsync(id);
        if (events == null)
        {
            return NotFound();
        }

        _context.Events.Remove(events);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool EventsExists(int? id)
    {
        return _context.Events.Any(e => e.Id == id);
    }
}
