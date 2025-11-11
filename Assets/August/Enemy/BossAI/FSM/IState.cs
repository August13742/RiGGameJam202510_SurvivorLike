using System;

namespace Survivor.Enemy.FSM
{
    

    public interface IState
    {
        void Enter();
        Type Tick(float deltaTime);
        void Exit();
    }
}