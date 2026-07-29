using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Electrons.Net8.Api.Models;
using Electrons.Net8.Api;

[Route("api/[controller]")]
[ApiController]
public class AwardsController(ElectronsDbContext context) : ControllerBase
{
    private readonly ElectronsDbContext _context = context;

    // GET: api/Awards
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Award>>> GetAwards()
    {
        return await _context.Awards.ToListAsync();
    }

    // GET: api/Awards/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Award>> GetAwards(int id)
    {
        var awards = await _context.Awards.FindAsync(id);

        if (awards == null)
        {
            return NotFound();
        }

        return awards;
    }

    // PUT: api/Awards/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAwards(int? id, Award awards)
    {
        if (id != awards.Id)
        {
            return BadRequest();
        }

        _context.Entry(awards).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AwardsExists(id))
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

    // POST: api/Awards
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Award>> PostAwards(Award awards)
    {
        _context.Awards.Add(awards);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAwards", new { id = awards.Id }, awards);
    }

    // DELETE: api/Awards/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAwards(int? id)
    {
        var awards = await _context.Awards.FindAsync(id);
        if (awards == null)
        {
            return NotFound();
        }

        _context.Awards.Remove(awards);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool AwardsExists(int? id)
    {
        return _context.Awards.Any(e => e.Id == id);
    }
}
