using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using EDCApp.Models;

namespace EDCApp.Services
{
    public class TrialService
    {
        public IOrganizationService? ServiceClient { get; set; }

        public async Task<List<Trial>> GetTrialsAsync()
        {
            if (ServiceClient == null)
            {
                throw new InvalidOperationException("ServiceClient is not initialized.");
            }

            var query = new QueryExpression("new_trial")
            {
                ColumnSet = new ColumnSet("new_trialid", "new_trial_name", "new_sponsor", "new_start_date", "new_end_date")
            };

            var result = await Task.Run(() => ServiceClient.RetrieveMultiple(query));

            var trials = new List<Trial>();
            foreach (var entity in result.Entities)
            {
                trials.Add(new Trial
                {
                    Id = entity.GetAttributeValue<Guid>("new_trialid"),
                    TrialName = entity.GetAttributeValue<string>("new_trial_name"),
                    Sponsor = entity.GetAttributeValue<string>("new_sponsor"),
                    StartDate = entity.GetAttributeValue<DateTime?>("new_start_date"),
                    EndDate = entity.GetAttributeValue<DateTime?>("new_end_date")
                });
            }

            return trials;
        }

        public async Task CreateTrialAsync(Trial trial)
        {
            if (ServiceClient == null)
            {
                throw new InvalidOperationException("ServiceClient is not initialized.");
            }
            
            var entity = new Entity("new_trial")
            {
                ["new_trial_name"] = trial.TrialName,
                ["new_sponsor"] = trial.Sponsor,
                ["new_start_date"] = trial.StartDate,
                ["new_end_date"] = trial.EndDate
            };

            await Task.Run(() => ServiceClient.Create(entity));
        }

        public async Task UpdateTrialAsync(Trial trial)
        {
            if (ServiceClient == null)
            {
                throw new InvalidOperationException("ServiceClient is not initialized.");
            }
            
            var entity = new Entity("new_trial", trial.Id)
            {
                ["new_trial_name"] = trial.TrialName,
                ["new_sponsor"] = trial.Sponsor,
                ["new_start_date"] = trial.StartDate,
                ["new_end_date"] = trial.EndDate
            };

            await Task.Run(() => ServiceClient.Update(entity));
        }

        public async Task DeleteTrialAsync(Guid id)
        {
            if (ServiceClient == null)
            {
                throw new InvalidOperationException("ServiceClient is not initialized.");
            }
            
            await Task.Run(() => ServiceClient.Delete("new_trial", id));
        }
    }
}
