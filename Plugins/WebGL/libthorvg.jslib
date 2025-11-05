var ThorVGPlugin = {
    $ThorVG: {
        module: null,
        animations: {},
        nextId: 1,
        state: 0
    },
    
    ThorVG_IsReady__deps: ['$ThorVG'],
    ThorVG_IsReady: function() {
        return ThorVG.state === 2 ? 1 : (ThorVG.state < 0 ? -1 : 0);
    },
    
    ThorVG_Init__deps: ['$ThorVG'],
    ThorVG_Init: function() {
        if (ThorVG.state === 2) return 1;
        if (ThorVG.state !== 0) return 0;
        
        ThorVG.state = 1;
        
        var script = document.createElement('script');
        script.type = 'module';
        // Load from package StreamingAssets (copied during build)
        script.textContent = `(async () => {
            try {
                const ThorVGModule = await import('./StreamingAssets/Packages/com.thorvg.unity/WebGL/thorvg.js');
                const module = await ThorVGModule.default();
                module.init();
                window.$ThorVGModule = module;
                window.$ThorVGReady && window.$ThorVGReady(true);
            } catch {
                window.$ThorVGReady && window.$ThorVGReady(false);
            }
        })();`;
        document.head.appendChild(script);
        
        window.$ThorVGReady = function(success) {
            if (success) {
                ThorVG.module = window.$ThorVGModule;
                ThorVG.state = 2;
                delete window.$ThorVGModule;
            } else {
                ThorVG.state = -1;
            }
            delete window.$ThorVGReady;
        };
        
        return 0;
    },
    
    ThorVG_Term__deps: ['$ThorVG'],
    ThorVG_Term: function() {
        if (ThorVG.module) {
            ThorVG.module.term();
        }
    },
    
    ThorVG_CreateAnimation__deps: ['$ThorVG'],
    ThorVG_CreateAnimation: function(dataPtr) {
        if (!ThorVG.module) return 0;
        
        try {
            var animation = new ThorVG.module.TvgLottieAnimation("sw", "");
            animation.load(UTF8ToString(dataPtr), "", 32, 32);
            var id = ThorVG.nextId++;
            ThorVG.animations[id] = animation;
            return id;
        } catch {
            return 0;
        }
    },
    
    ThorVG_DestroyAnimation__deps: ['$ThorVG'],
    ThorVG_DestroyAnimation: function(id) {
        var anim = ThorVG.animations[id];
        if (anim) {
            anim.delete();
            delete ThorVG.animations[id];
        }
    },
    
    ThorVG_GetSize__deps: ['$ThorVG'],
    ThorVG_GetSize: function(id, outWidth, outHeight) {
        var anim = ThorVG.animations[id];
        if (!anim) return 1;
        
        var size = anim.size();
        HEAPF32[outWidth >> 2] = size[0];
        HEAPF32[outHeight >> 2] = size[1];
        return 0;
    },
    
    ThorVG_GetDuration__deps: ['$ThorVG'],
    ThorVG_GetDuration: function(id) {
        var anim = ThorVG.animations[id];
        return anim ? anim.duration() : 0;
    },
    
    ThorVG_GetTotalFrame__deps: ['$ThorVG'],
    ThorVG_GetTotalFrame: function(id) {
        var anim = ThorVG.animations[id];
        return anim ? anim.totalFrame() : 0;
    },
    
    ThorVG_SetFrame__deps: ['$ThorVG'],
    ThorVG_SetFrame: function(id, frame) {
        var anim = ThorVG.animations[id];
        if (!anim) return 1;
        anim.frame(frame);
        return 0;
    },
    
    ThorVG_Resize__deps: ['$ThorVG'],
    ThorVG_Resize: function(id, width, height) {
        var anim = ThorVG.animations[id];
        if (!anim) return 1;
        anim.resize(width, height);
        return 0;
    },
    
    ThorVG_RenderToBuffer__deps: ['$ThorVG'],
    ThorVG_RenderToBuffer: function(id, bufferPtr, bufferSize) {
        var anim = ThorVG.animations[id];
        if (!anim) return 1;
        
        try {
            anim.update();
            var renderBuffer = anim.render();
            var srcView = new Uint8Array(renderBuffer);
            var dstView = new Uint8Array(HEAPU8.buffer, bufferPtr, bufferSize);
            
            if (srcView.length <= bufferSize) {
                dstView.set(srcView);
                return 0;
            }
            return 1;
        } catch {
            return 1;
        }
    }
};

autoAddDeps(ThorVGPlugin, '$ThorVG');
mergeInto(LibraryManager.library, ThorVGPlugin);
