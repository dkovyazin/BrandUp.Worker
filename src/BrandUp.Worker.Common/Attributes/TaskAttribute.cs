﻿using System;

namespace BrandUp.Worker
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TaskAttribute : Attribute
    {
        public string Name { get; set; }
        public int TimeoutWaitingToStartInMiliseconds { get; set; }
        public int ExecutionTimeout { get; set; } = 30000;
    }
}