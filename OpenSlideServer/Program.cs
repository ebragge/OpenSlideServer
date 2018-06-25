// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace OpenSlideServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting ...");
            new ImageServer();
        }
    }
}
