/***************************************************************************
 *  ComplexMenuItem.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using Gtk;

namespace Banter
{
    public class ComplexMenuItem : MenuItem
    {
        private bool is_selected;
        private bool interactive;
        private EventBox selected_widget;
        private ArrayList children = new ArrayList();
        private ArrayList input_children = new ArrayList();

        public ComplexMenuItem() : this(false)
        {
        }
        
        public ComplexMenuItem(bool interactive) : base()
        {
            this.is_selected = false;
            this.interactive = interactive;
        }

        protected Widget RegisterWidget(Widget widget)
        {
            if(widget is Button) {
                ((Button)widget).Relief = ReliefStyle.None;
            }
        
            EventBox box = new EventBox();
            box.AppPaintable = true;
            box.Add(widget);
            ConnectChildExpose(box);
            if(!(widget is Label)) {
                input_children.Add(box);
            }
            
            return box;
        }
        
        protected void ConnectChildExpose(Widget widget)
        {
            children.Add(widget);
            widget.ExposeEvent += OnChildExposeEvent;
        }

        [GLib.ConnectBefore]
        private void OnChildExposeEvent(object o, ExposeEventArgs args)
        {
            if(interactive) {
                InteractiveExpose((Widget)o, args.Event);
            } else {
                StaticExpose((Widget)o, args.Event);
            }
        }
        
        private void InteractiveExpose(Widget widget, Gdk.EventExpose evnt)
        {
            widget.GdkWindow.DrawRectangle(Parent.Style.BackgroundGC(StateType.Normal), 
                true, 0, 0, widget.Allocation.Width, widget.Allocation.Height);
                    
            int x = Parent.Allocation.X - widget.Allocation.X;
            int y = Parent.Allocation.Y - widget.Allocation.Y;
            int width = Parent.Allocation.Width;
            int height = Parent.Allocation.Height;
                
            Gtk.Style.PaintBox(Style, widget.GdkWindow, StateType.Normal, ShadowType.Out,
                evnt.Area, widget, "menu", x, y, width, height);
        
            if(SelectedWidget == widget && ((EventBox)widget).Child is Button) {
                ButtonExpose((EventBox)widget, evnt);
            }
        }
        
        private void ButtonExpose(EventBox widget, Gdk.EventExpose evnt)
        {
            if(widget.Child is ToggleButton && ((ToggleButton)widget.Child).Active) {
                return;
            }

            ShadowType shadow_type = (ShadowType)StyleGetProperty("selected-shadow-type");
            Gtk.Style.PaintBox(Style, widget.GdkWindow, StateType.Prelight, shadow_type,
                evnt.Area, widget, "menuitem", 0, 0, 
                widget.Allocation.Width, widget.Allocation.Height);
        }
        
        private void StaticExpose(Widget widget, Gdk.EventExpose evnt)
        {
            // NOTE: This is a little insane, but it allows packing of EventBox based widgets
            // into a GtkMenuItem without breaking the theme (leaving an unstyled void in the item).
            // This method is called before the EventBox child does its drawing and the background
            // is filled in with the proper style.
            
            int x, y, width, height;

            if(IsSelected) {
                x = Allocation.X - widget.Allocation.X;
                y = Allocation.Y - widget.Allocation.Y;
                width = Allocation.Width;
                height = Allocation.Height;
                
                ShadowType shadow_type = (ShadowType)StyleGetProperty("selected-shadow-type");
                Gtk.Style.PaintBox(Style, widget.GdkWindow, StateType.Prelight, shadow_type,
                    evnt.Area, widget, "menuitem", x, y, width, height);
            } else {
                // Fill only the visible area in solid color, to be most efficient
                widget.GdkWindow.DrawRectangle(Parent.Style.BackgroundGC(StateType.Normal), 
                    true, 0, 0, widget.Allocation.Width, widget.Allocation.Height);
               
                // FIXME: The above should not be necessary, but Clearlooks-based themes apparently 
                // don't provide any style for the menu background so we have to fill it first with 
                // the correct theme color. Weak.
                //
                // Do a complete style paint based on the size of the entire menu to be compatible with
                // themes that provide a real style for "menu"
                x = Parent.Allocation.X - widget.Allocation.X;
                y = Parent.Allocation.Y - widget.Allocation.Y;
                width = Parent.Allocation.Width;
                height = Parent.Allocation.Height;
                
                Gtk.Style.PaintBox(Style, widget.GdkWindow, StateType.Normal, ShadowType.Out,
                    evnt.Area, widget, "menu", x, y, width, height);
            }
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!interactive) {
                return base.OnExposeEvent(evnt);
            }

            return true;
        }
        
        protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
        {
            SelectedWidget = null;
            
            foreach(EventBox child in children) {
                int x = child.Allocation.X;
                int y = child.Allocation.Y;
                int width = child.Allocation.Width;
                int height = child.Allocation.Height;
                int e_x = (int)evnt.X + Allocation.X;
                int e_y = (int)evnt.Y + Allocation.Y;

                if(e_x >= x && e_x <= x + width &&
                    e_y >= y && e_y <= y + height) {
                    SelectedWidget = child;
                    QueueDraw();
                    break;
                }
            }
            
            if(SelectedWidget == null) {
                QueueDraw();
            }
            
            return base.OnMotionNotifyEvent(evnt);
        }
        
        protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
        {
            SelectedWidget = null;
            QueueDraw();
            return base.OnLeaveNotifyEvent(evnt);
        }
        
        protected override void OnSelected()
        {
            base.OnSelected();
            is_selected = true;
            
            if(Gtk.Global.CurrentEvent is Gdk.EventKey && input_children.Count > 0) {
                SelectedWidget = (EventBox)input_children[0];
            }
        }
        
        protected override void OnDeselected()
        {
            base.OnDeselected();
            is_selected = false;
            SelectedWidget = null;
        }
        
        protected override void OnStateChanged(StateType previous_state)
        {
            if(State == StateType.Prelight && interactive) {
                State = StateType.Normal;
            } else if(!interactive) {
                base.OnStateChanged(previous_state);
            }
        }
        
        protected override void OnParentSet(Widget previous_parent)
        {
            if(previous_parent != null) {
                previous_parent.KeyPressEvent -= OnKeyPressEventProxy;
            }
            
            if(Parent != null) {
                Parent.KeyPressEvent += OnKeyPressEventProxy;
            }
        }
        
        [GLib.ConnectBefore]
        private void OnKeyPressEventProxy(object o, KeyPressEventArgs args)
        {
            if(!IsSelected) {
                return;
            }

            switch(args.Event.Key) {
                case Gdk.Key.Up:
                case Gdk.Key.Down:
                case Gdk.Key.Escape:
                    return;
            }

            args.RetVal = OnKeyPressEvent(args.Event);
        }

        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
            if(!interactive) {
                return base.OnButtonReleaseEvent(evnt);
            }
            
            if(SelectedWidget != null) {
                ActivateWidget((EventBox)SelectedWidget);
            }
            
            return true;
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            if(!interactive) {
                return false;
            }

            if(evnt.Key == Gdk.Key.Left || evnt.Key == Gdk.Key.Right) {            
                int index = input_children.IndexOf(SelectedWidget);
                
                if(index < 0) {
                    index = 0;
                } else {
                    index += evnt.Key == Gdk.Key.Left ? -1 : 1;
                }
                
                if(index >= input_children.Count) {
                    index = 0;
                } else if(index < 0) {
                    index = input_children.Count -1;
                }
                
                SelectedWidget = (EventBox)input_children[index];
                QueueDraw();
            } else if(evnt.Key == Gdk.Key.Return && SelectedWidget != null) {
                ActivateWidget((EventBox)SelectedWidget);
            }
            
            return true;
        }
        
        private void ActivateWidget(EventBox widget)
        {
            if(widget.Child is Button) {
                ((Button)widget.Child).Activate();
            }
        }
        
        protected bool IsSelected {
            get { return is_selected; }
        }
        
        protected EventBox SelectedWidget {
            get { return selected_widget; }
            set {
                if(selected_widget == value) {
                    return;
                }
                
                if(selected_widget != null && selected_widget.State != StateType.Normal) {
                    selected_widget.State = StateType.Normal;
                }
                
                selected_widget = value;
                if(selected_widget != null && selected_widget.State != StateType.Active) {
                    selected_widget.State = StateType.Active;
                }
            }
        }
        
        public bool DrawSelection {
            get { return interactive; }
            set { interactive = value; }
        }
    }
}
