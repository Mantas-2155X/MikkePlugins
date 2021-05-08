using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoveController
{
    class MoveAndRotateAddCommand : Studio.ICommand
    {
        public GuideCommand.RotationAddCommand rotateCom;
        public GuideCommand.MoveAddCommand moveCom;

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

    class MoveAndRotateEqualsCommand : Studio.ICommand
    {
        public GuideCommand.RotationEqualsCommand rotateCom;
        public GuideCommand.MoveAddCommand moveCom;

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
