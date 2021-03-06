﻿namespace Assent.Tests.EndToEnd
{
    public class SanitiserTransformsTheRecievedFileScenario : BaseScenario
    {
       private readonly string _approvedPath =
            $@"{GetTestDirectory()}\EndToEnd\{nameof(SanitiserTransformsTheRecievedFileScenario)}.{nameof(WhenTheTestIsRun)}.approved.txt";

        
        public void AndGivenADelegateSanitiser()
        {
            Configuration = Configuration.UsingSanitiser(s => s.Replace(" ", "_"));
        }

        public void AndGivenTheApprovedFileHasTheSanitisedVersion()
        {
            ReaderWriter.Files[_approvedPath] = "Foo_Bar";
        }
        

        public void WhenTheTestIsRun()
        {
            this.Assent("Foo Bar", Configuration);
        }

        public void ThenNoExceptionIsThrown()
        {

        }
    }
}