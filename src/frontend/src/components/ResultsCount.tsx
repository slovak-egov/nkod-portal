type Props = {
    count: number;
}

export default function ResultsCount(props: Props) {
    return <>
        {props.count} {props.count === 1 ? 'výsledok' : props.count > 1 && props.count < 5 ? 'výsledky' : 'výsledkov'}
    </>
}