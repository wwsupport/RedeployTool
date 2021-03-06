using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchestrA.GRAccess;

namespace RedeployTester
{
    class Program
    {
        static void Main(string[] args)
        {
            int delayTime = Properties.Settings.Default.delay;
            string[] configuredinstances = Properties.Settings.Default.objects.Split(';');
            GRAccessApp grAccess = new GRAccessAppClass();
            string galaxyName = "";
            string galaxyUser = "";
            string galaxyPass = "";
            string cascade = "";
            bool _cascade = false;
            int loops = 1;

            ICommandResult cmd;

            Console.WriteLine("Initializing with {0}ms delay - deploying/undeploying {1}", delayTime, Properties.Settings.Default.objects);

            string nodeName = Environment.MachineName;
            Console.Write("Please enter galaxy name (Press return afterwards): ");
            galaxyName = Console.ReadLine();
            Console.Write("Please enter galaxy username, leave blank if none (Press return afterwards): ");
            galaxyUser = Console.ReadLine();
            Console.Write("Please enter galaxy password, leave blank if none (Press return afterwards): ");
            galaxyPass = Console.ReadLine();
            Console.Write("Please enter the number of loops you wish to run (Press return afterwards): ");
            loops = int.Parse(Console.ReadLine());
            Console.Write("Would you like to cascade deploy objects? (Y/N)?: ");
            cascade = Console.ReadLine();

            if ((cascade == "Y") || (cascade == "y"))
            {
                _cascade = true;
            }
            else { 
                _cascade = false;
            }


            Console.WriteLine("Fetching all galaxies on {0}", nodeName);
            IGalaxies gals = grAccess.QueryGalaxies(nodeName);

            if (gals == null || grAccess.CommandResult.Successful == false)
            {
                Console.WriteLine(grAccess.CommandResult.CustomMessage + grAccess.CommandResult.Text);
                return;
            }
            
            IGalaxy galaxy = gals[galaxyName];

            if (galaxy == null)
            {
                Console.WriteLine("Failure, galaxy '{0}' does not exist", galaxyName);
                return;
            }
            Console.WriteLine("Attempting to log into galaxy {0}", galaxyName);
            galaxy.Login(galaxyUser, galaxyPass);

            cmd = galaxy.CommandResult;
            if (!cmd.Successful)
            {
                Console.WriteLine("Login to galaxy Example1 Failed :" + cmd.Text + " : " + cmd.CustomMessage);
                return;
            }

            IConditions conditions;
            
            conditions = galaxy.CreateConditionsObject();

            conditions.Add(EConditionType.NameEquals, "bob", true);

            conditions = null;


            List<IInstance> instances = new List<IInstance>();
            

            foreach (var cinstance in configuredinstances)
            {

                string[] specifics = cinstance.Split(',');
                conditions = galaxy.CreateConditionsObject();

                conditions.Add(EConditionType.NameEquals, specifics[0], true);
                conditions.Add(EConditionType.NameSpaceIdIs, specifics[1], true);
                IgObjects queryObjects = galaxy.QueryObjectsMultiCondition(EgObjectIsTemplateOrInstance.gObjectIsInstance, conditions);
                cmd = galaxy.CommandResult;
                if (!cmd.Successful)
                {
                    Console.WriteLine("Failed to find object(s) " + cmd.Text + " : " + cmd.CustomMessage); ;
                    return;
                }
                foreach (IInstance item in queryObjects)
                {
                    instances.Add(item);
                }
                
                conditions = null;

            }
            

            //IgObjects queryObjects = galaxy.QueryObjectsMultiCondition(EgObjectIsTemplateOrInstance.gObjectIsInstance, ref objects);
            

            //cmd = galaxy.CommandResult;

            //if (!cmd.Successful)
            //{
            //    Console.WriteLine("Failed to find object(s) " + cmd.Text + " : " + cmd.CustomMessage); ;
            //    return;
            //}


            for (int i = 1; i <= loops; i++)
            {
                Console.WriteLine("Loop {0}", i);
                foreach (IInstance instance in instances)
                {
                    Console.WriteLine("Deploying {0}", instance.Tagname);
                    if (!_cascade)
                    {
                        instance.Deploy(EActionForCurrentlyDeployedObjects.skipDeploy, ESkipIfCurrentlyUndeployed.dontSkipIfCurrentlyUndeployed, EDeployOnScan.doDeployOnScan, EForceOffScan.doForceOffScan, ECascade.dontCascade);
                    }
                    else
                    {
                        instance.Deploy(EActionForCurrentlyDeployedObjects.skipDeploy, ESkipIfCurrentlyUndeployed.dontSkipIfCurrentlyUndeployed, EDeployOnScan.doDeployOnScan, EForceOffScan.doForceOffScan, ECascade.doCascade);
                    }
                }
                Console.WriteLine("Waiting {0}ms", delayTime);
                Thread.Sleep(delayTime);
                foreach (IInstance instance in instances)
                {
                    Console.WriteLine("Un-Deploying {0}", instance.Tagname);
                    if (!_cascade)
                    {
                        instance.Undeploy(EForceOffScan.doForceOffScan, ECascade.doCascade);
                    }
                    else
                    {
                        instance.Undeploy(EForceOffScan.doForceOffScan, ECascade.doCascade);
                    }
                }
                Thread.Sleep(delayTime);
                Console.WriteLine("Waiting {0}ms", delayTime);
            }

            Console.WriteLine("All loops complete, press return to close the application.");
            Console.ReadLine();

        }
    }
}
