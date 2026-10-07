type Props = {
    numerator?: number | null | undefined;
    denominator?: number | null | undefined;
};

export default function Indicator(props: Props) {
    const { numerator, denominator } = props;
    const useDenominator = typeof denominator === 'number' ? denominator : 0;
    const share = typeof numerator === 'number' && useDenominator > 0 ? Math.min(Math.max(numerator / useDenominator, 0), 1) : undefined;
    const percent = share !== undefined ? Math.round(share * 100) : undefined;
    const color = percent !== undefined ? (percent >= 80 ? 'green' : percent >= 40 ? 'yellow' : 'red') : 'red';

    let count = percent !== undefined ? Math.ceil(percent / 20) : 0;
    if (count < 1) {
        count = 1;
    }
    if (count > 5) {
        count = 5;
    }

    const label = percent !== undefined ? (percent >= 100 ? 'OK' : `${percent}%`) : 'N/A';

    return (
        <>
            {percent !== undefined ? (
                <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap' }}>
                    {count > 0 ? (
                        <div style={{ display: 'flex', alignItems: 'center', marginRight: '6px' }}>
                            {Array.from({ length: count }).map((_, index) => (
                                <div
                                    key={index}
                                    style={{ width: '12px', height: '12px', backgroundColor: color, display: 'inline-block', marginRight: '2px' }}
                                ></div>
                            ))}
                        </div>
                    ) : null}
                    <div style={{ whiteSpace: 'nowrap' }}>{label}</div>
                </div>
            ) : (
                'N/A'
            )}
        </>
    );
}
