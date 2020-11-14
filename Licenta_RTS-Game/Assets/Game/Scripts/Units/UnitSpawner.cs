using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lockstep;
using FastCollections;

namespace MariusRTS
{
    public class UnitSpawner : BehaviourHelper
    {
        public override ushort ListenInput
        {
            get
            {
                return InputCodeManager.GetCodeID(_inputCode);
            }
        }
        [SerializeField, DataCode("Input")]
        private string _inputCode;
        [SerializeField, DataCode("Agents")]
        private string _agentSpawnCode;

        protected override void OnVisualize()
        {
            if (Input.GetKeyDown(KeyCode.Space) && false)
            {
                var com = GetSpawnCommand(PlayerManager.MainController, _agentSpawnCode, RTSInterfacing.GetWorldPosD(Input.mousePosition));
                CommandManager.SendCommand(com);
            }
        }

        protected override void OnExecute(Command com)
        {
            ProcessSpawnCommand(com);
        }

        public Command GetSpawnCommand(AgentController agentController, string agentCode, Vector2d position)
        {
            Command com = new Command();
            com.InputCode = InputCodeManager.GetCodeID(_inputCode);
            com.ControllerID = agentController.ControllerID;
            com.Add(new DefaultData(agentCode));
            com.Add(position);
            return com;
        }

        void ProcessSpawnCommand(Command com)
        {
            var controllerCode = com.ControllerID;
            var agentCode = (string)com.GetData<DefaultData>().Value;
            var position = com.GetData<Vector2d>();

            SpawnUnit(AgentController.GetInstanceManager(controllerCode), agentCode, position);
        }

        public LSAgent SpawnUnit (AgentController agentController, string agentCode, Vector2d position)
        {
            return agentController.CreateAgent(agentCode, position);
        }
    }
}
