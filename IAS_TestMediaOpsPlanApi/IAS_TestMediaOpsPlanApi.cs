/*
****************************************************************************
*  Copyright (c) 2025,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

24/11/2025	1.0.0.1		TRE, Skyline	Initial version
****************************************************************************
*/

namespace IASTestMediaOpsPlanApi
{
    using System;
    using IAS_TestMediaOpsPlanApi;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Automation;
    using Skyline.DataMiner.Utils.InteractiveAutomationScript;
    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        private InteractiveController app;
        private IMediaOpsPlanApi planApi;

        /// <summary>
        /// The Script entry point.
        /// IEngine.ShowUI();.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                app = new InteractiveController(engine);
                planApi = engine.GetMediaOpsPlanApi();

                engine.SetFlag(RunTimeFlags.NoKeyCaching);
                engine.Timeout = TimeSpan.FromHours(10);

                RunSafe(engine);
            }
            catch (ScriptAbortException)
            {
                throw;
            }
            catch (ScriptForceAbortException)
            {
                throw;
            }
            catch (ScriptTimeoutException)
            {
                throw;
            }
            catch (InteractiveUserDetachedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                engine.Log($"Run|Something went wrong: {ex}");
            }
        }

        private void RunSafe(IEngine engine)
        {
            CreateResourceDialog dialog = new CreateResourceDialog(engine);
            dialog.ResourceNameTextBox.Text = $"Green Resource [{Guid.NewGuid()}]";
            dialog.Button.Pressed += (s, e) =>
            {
                try
                {
                    var resourceId = planApi.Resources.Create(new UnmanagedResource
                    {
                        Name = dialog.ResourceNameTextBox.Text
                    });

                    dialog.ResultsTextBox.Text = $"[{DateTime.Now}] Resource created with ID: {resourceId}";
                }
                catch (Exception ex)
                {
                    dialog.ResultsTextBox.Text = $"[{DateTime.Now}] Error creating resource: {ex.Message}";
                }
            };

            app.ShowDialog(dialog);
        }
    }
}
