using Microsoft.EntityFrameworkCore;
using TechSpecs.Models;

namespace TechSpecs.Data;

public static class DataFixer
{
    public static async Task FixDataAsync(AppDbContext db)
    {
        // 1. Motherboards
        var motherboards = await db.Motherboards.ToListAsync();
        foreach (var m in motherboards)
        {
            var name = m.Name.ToUpper();
            if (string.IsNullOrEmpty(m.Chipset) || m.Chipset == "Unknown")
            {
                if (name.Contains("Z890")) m.Chipset = "Z890";
                else if (name.Contains("Z790")) m.Chipset = "Z790";
                else if (name.Contains("B760")) m.Chipset = "B760";
                else if (name.Contains("H610")) m.Chipset = "H610";
                else if (name.Contains("X670E")) m.Chipset = "X670E";
                else if (name.Contains("X670")) m.Chipset = "X670";
                else if (name.Contains("B650E")) m.Chipset = "B650E";
                else if (name.Contains("B650")) m.Chipset = "B650";
                else if (name.Contains("A620")) m.Chipset = "A620";
                else if (name.Contains("X870E")) m.Chipset = "X870E";
                else if (name.Contains("X870")) m.Chipset = "X870";
                else if (name.Contains("Z690")) m.Chipset = "Z690";
                else if (name.Contains("B660")) m.Chipset = "B660";
                else if (name.Contains("TRX50")) m.Chipset = "TRX50";
            }
            if (name.Contains("E-ATX") || name.Contains("EATX"))
            {
                m.FormFactor = "eATX";
            }
        }

        // 2. RAM (Memories)
        var memories = await db.Memories.ToListAsync();
        foreach (var mem in memories)
        {
            var name = mem.Name.ToUpper();
            if (name.Contains("EXPO"))
            {
                mem.Profile = "AMD Expo";
            }
            else if (name.Contains("XMP"))
            {
                mem.Profile = "Intel XMP";
            }
        }

        // 3. Storage
        var storages = await db.Storages.ToListAsync();
        foreach (var s in storages)
        {
            var name = s.Name.ToUpper();
            if (s.Type == "NVMe")
            {
                s.Type = "SSD";
            }
            if (s.Interface == "M.2")
            {
                if (name.Contains("GEN5") || name.Contains("GEN 5") || name.Contains("PCIE 5.0"))
                    s.Interface = "PCIe Gen 5";
                else if (name.Contains("GEN4") || name.Contains("GEN 4") || name.Contains("PCIE 4.0"))
                    s.Interface = "PCIe Gen 4";
                else
                    s.Interface = "PCIe Gen 3";
            }
        }

        // 4. Power Supply (PSU)
        var psus = await db.PowerSupplies.ToListAsync();
        foreach (var p in psus)
        {
            var name = p.Name.ToUpper();
            if (name.Contains("SFX-L"))
                p.PsuFormFactor = "SFX-L";
            else if (name.Contains("SFX"))
                p.PsuFormFactor = "SFX";
            else
                p.PsuFormFactor = "ATX";
        }

        // 5. Case (CaseEnclosure)
        var cases = await db.CaseEnclosures.ToListAsync();
        foreach (var c in cases)
        {
            var name = c.Name.ToUpper();
            if (name.Contains("MINI") || name.Contains("ITX"))
                c.CaseType = "Mini-Tower";
            else if (name.Contains("FULL"))
                c.CaseType = "Full-Tower";
            else
                c.CaseType = "Mid-Tower";

            if (name.Contains("E-ATX") || name.Contains("EATX"))
            {
                if (!string.IsNullOrEmpty(c.FormFactorSupport) && !c.FormFactorSupport.Contains("eATX"))
                    c.FormFactorSupport += ", eATX";
                else if (string.IsNullOrEmpty(c.FormFactorSupport))
                    c.FormFactorSupport = "eATX";
            }
        }

        await db.SaveChangesAsync();
    }
}
