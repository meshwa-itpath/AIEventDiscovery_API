using System.Collections.Generic;

namespace AIEventDiscovery.DTOs;

public class OnBoardingDetailRequestDto
{
    public string? Role { get; set; }
    
    public List<string>? Technology { get; set; }
}
