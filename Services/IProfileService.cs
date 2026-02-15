using System.Collections.Generic;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public interface IProfileService
{
    IEnumerable<ParsingProfile> GetAllProfiles();
    ParsingProfile GetActiveProfile();
    void SetActiveProfile(string name);
    void SaveProfile(ParsingProfile profile);
    void DeleteProfile(string name);
}
