import { useEffect, useRef, useState } from 'react';
import { MapEmbedProps } from '../client';

function clamp(value: number, min?: number | null, max?: number | null): number {
    if (min != null && max != null && min > max) {
        return Math.floor(value);
    }

    let result = value;
    if (max != null) result = Math.min(result, max);
    if (min != null) result = Math.max(result, min);
    return Math.floor(result);
}

export function MapEmbed(props: MapEmbedProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const [size, setSize] = useState({ width: 0, height: 0 });

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const resize = () => {
            const mw = container.clientWidth;
            const mh = window.innerHeight - 50;

            let w = mw;
            let h = mh;

            if (w <= 0 || h <= 0) return;

            if (props.recommendedWidth !== null && props.recommendedWidth !== undefined && props.recommendedWidth > 0 && props.recommendedWidth <= mw) {
                w = props.recommendedWidth;
            }

            if (props.recommendedHeight !== null && props.recommendedHeight !== undefined && props.recommendedHeight > 0 && props.recommendedHeight <= mh) {
                h = props.recommendedHeight;
            }

            w = clamp(w, props.minWidth, props.maxWidth);
            h = clamp(h, props.minHeight, props.maxHeight);

            if (props.recommendedAspectRatio && w > 0 && h > 0) {
                const [rw, rh] = props.recommendedAspectRatio.split(':').map(Number);
                if (rw > 0 && rh > 0) {
                    const aspectRatio = rw / rh;
                    if (w / h > aspectRatio) {
                        w = h * aspectRatio;
                    } else {
                        h = w / aspectRatio;
                    }
                }
            }

            w = clamp(w, null, mw);
            h = clamp(h, null, mh);

            setSize({
                width: w,
                height: h
            });
        };

        resize();
        const observer = new ResizeObserver(resize);
        observer.observe(container);
        window.addEventListener('resize', resize);

        return () => {
            observer.disconnect();
            window.removeEventListener('resize', resize);
        };
    }, [props]);

    if (!props.mapEmbedUrl) return null;

    return (
        <div ref={containerRef} style={{ width: '100%', overflowX: 'auto' }}>
            {size.width > 0 && size.height > 0 ? (
                <iframe
                    src={props.mapEmbedUrl}
                    title="Map"
                    width={size.width}
                    height={size.height}
                    loading="lazy"
                    allow=""
                    sandbox="allow-scripts allow-forms"
                    referrerPolicy="no-referrer"
                    style={{ border: 0, display: 'block' }}
                />
            ) : null}
        </div>
    );
}
