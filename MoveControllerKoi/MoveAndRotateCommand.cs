using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoveController
{
    public class MoveAndRotateAddCommand : ICommand
    {
        private readonly GuideCommand.RotationAddCommand rotateCom;
        public readonly GuideCommand.MoveAddCommand moveCom;

        public MoveAndRotateAddCommand(GuideCommand.RotationAddCommand rotateCom, GuideCommand.MoveAddCommand moveCom)
        {
            this.rotateCom = rotateCom;
            this.moveCom = moveCom;
        }
            
        public void Do()
        {
            rotateCom.Do();
            moveCom.Do();
        }

        public void Redo()
        {
            rotateCom.Redo();
            moveCom.Redo();
        }

        public void Undo()
        {
            rotateCom.Undo();
            moveCom.Undo();
        }
    }

    public class MoveAndRotateEqualsCommand : ICommand
    {
        private readonly GuideCommand.RotationEqualsCommand rotateCom;
        private readonly GuideCommand.MoveAddCommand moveCom;

        public MoveAndRotateEqualsCommand(GuideCommand.RotationEqualsCommand rotateCom, GuideCommand.MoveAddCommand moveCom)
        {
            this.rotateCom = rotateCom;
            this.moveCom = moveCom;
        }

        public void Do()
        {
            rotateCom.Do();
            moveCom.Undo();
        }

        public void Redo()
        {
            rotateCom.Redo();
            moveCom.Undo();
        }

        public void Undo()
        {
            rotateCom.Undo();
            moveCom.Do();
        }
    }
}
