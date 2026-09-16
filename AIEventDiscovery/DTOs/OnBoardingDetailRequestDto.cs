using System.Collections.Generic;

namespace AIEventDiscovery.DTOs;

public class OnBoardingDetailRequestDto
{
    /// <summary>Step 1: Role / Specialization (Persona lens)</summary>
    public string? Role { get; set; }
    
    /// <summary>Step 2: Primary working stacks (e.g. .NET, Node.js, Python, Java)</summary>
    public List<string>? PrimaryStacks { get; set; } = [];

    /// <summary>Step 3: Areas of interest / topics (e.g. Cloud, DevOps, Vector DBs)</summary>
    public List<string>? Interests { get; set; } = [];
}
