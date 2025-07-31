using FSM;
using Prism.Ioc;
using SpinCoaterAndDeveloper.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.FSM.FSM
{
    public class MachineStateMachine : FSMAbsStateMachine
    {
        private readonly IContainerProvider containerProvider;

        public MachineStateMachine(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        public override List<FSMState> DeclareAllStates()
        {
            List<FSMState> stateList = new List<FSMState>();
            FSMState powerUppingState = new FSMState(FSMStateCode.PowerUpping);
            FSMState idlingState = new FSMState(FSMStateCode.Idling);
            FSMState globleResettingState = new FSMState(FSMStateCode.GlobleResetting);
            FSMState runningState = new FSMState(FSMStateCode.Running);
            FSMState pausingState = new FSMState(FSMStateCode.Pausing);
            FSMState emergencyStoppingState = new FSMState(FSMStateCode.EmergencyStopping);
            FSMState alarmingState = new FSMState(FSMStateCode.Alarming);
            FSMState burnInTestingState = new FSMState(FSMStateCode.BurnInTesting);
            FSMState burnInAlarmingState = new FSMState(FSMStateCode.BurnInAlarming);
            FSMState burnInPausingState = new FSMState(FSMStateCode.BurnInPausing);

            //添加PowerUpping状态对应的动作
            powerUppingState.AddTransition(new ResetTransition(containerProvider, powerUppingState, globleResettingState));
            powerUppingState.AddTransition(new EmergencyStopTransition(containerProvider, powerUppingState, emergencyStoppingState));
            //添加GlobleResetting状态对应的动作
            globleResettingState.AddTransition(new GlobleResetSuccessTransition(containerProvider, globleResettingState, idlingState));
            globleResettingState.AddTransition(new GlobleResetFailTransition(containerProvider, globleResettingState, emergencyStoppingState));
            globleResettingState.AddTransition(new EmergencyStopTransition(containerProvider, globleResettingState, emergencyStoppingState));
            globleResettingState.AddTransition(new StopTransition(containerProvider, globleResettingState, emergencyStoppingState));
            //添加Idling状态对应的动作
            idlingState.AddTransition(new StartUpTransition(containerProvider, idlingState, runningState));
            idlingState.AddTransition(new EmergencyStopTransition(containerProvider, idlingState, emergencyStoppingState));
            idlingState.AddTransition(new EnterBurnInTransition(containerProvider, idlingState, burnInTestingState));
            //添加Running状态的对应的动作
            runningState.AddTransition(new StopTransition(containerProvider, runningState, pausingState));
            runningState.AddTransition(new EmergencyStopTransition(containerProvider, runningState, emergencyStoppingState));
            runningState.AddTransition(new AlarmTransition(containerProvider, runningState, alarmingState));
            //添加Pausing状态对应的动作
            pausingState.AddTransition(new StartUpTransition(containerProvider, pausingState, runningState));
            pausingState.AddTransition(new EmergencyStopTransition(containerProvider, pausingState, emergencyStoppingState));
            pausingState.AddTransition(new ResetTransition(containerProvider, pausingState, globleResettingState));
            //添加EmergencyStopping状态对应的动作
            emergencyStoppingState.AddTransition(new ResetTransition(containerProvider, emergencyStoppingState, globleResettingState));
            emergencyStoppingState.AddTransition(new StopTransition(containerProvider, emergencyStoppingState, powerUppingState));
            //添加BurnInTesting状态对应的动作
            burnInTestingState.AddTransition(new LeaveBurnInTransition(containerProvider, burnInTestingState, powerUppingState));
            burnInTestingState.AddTransition(new EmergencyStopTransition(containerProvider, burnInTestingState, emergencyStoppingState));
            burnInTestingState.AddTransition(new AlarmTransition(containerProvider, burnInTestingState, burnInAlarmingState));
            burnInTestingState.AddTransition(new StopTransition(containerProvider, burnInTestingState, burnInPausingState));
            //添加Alarming状态对应的动作
            alarmingState.AddTransition(new StopTransition(containerProvider, alarmingState, pausingState));
            alarmingState.AddTransition(new StartUpTransition(containerProvider, alarmingState, runningState));
            alarmingState.AddTransition(new ResetTransition(containerProvider, alarmingState, globleResettingState));
            alarmingState.AddTransition(new EmergencyStopTransition(containerProvider, alarmingState, emergencyStoppingState));
            //空跑报警状态
            burnInAlarmingState.AddTransition(new EmergencyStopTransition(containerProvider, burnInAlarmingState, emergencyStoppingState));
            burnInAlarmingState.AddTransition(new StartUpTransition(containerProvider, burnInAlarmingState, burnInTestingState));
            burnInAlarmingState.AddTransition(new StopTransition(containerProvider, burnInAlarmingState, burnInPausingState));
            burnInAlarmingState.AddTransition(new ResetTransition(containerProvider, burnInAlarmingState, globleResettingState));
            //空跑暂停状态
            burnInPausingState.AddTransition(new EmergencyStopTransition(containerProvider, burnInPausingState, emergencyStoppingState));
            burnInPausingState.AddTransition(new StartUpTransition(containerProvider, burnInPausingState, burnInTestingState));
            burnInPausingState.AddTransition(new ResetTransition(containerProvider, burnInPausingState, globleResettingState));

            stateList.Add(powerUppingState);
            stateList.Add(globleResettingState);
            stateList.Add(idlingState);
            stateList.Add(runningState);
            stateList.Add(pausingState);
            stateList.Add(emergencyStoppingState);
            stateList.Add(alarmingState);
            stateList.Add(burnInTestingState);
            stateList.Add(burnInAlarmingState);
            stateList.Add(burnInPausingState);
            return stateList;
        }
    }
}
