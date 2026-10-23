            var dirs = new ConcurrentQueue<(string Path, int Depth)>();
            dirs.Enqueue((Source, 0));
            var walking = new[] { 1 };
            var workers = Agents();
            var tasks = new Task[workers];