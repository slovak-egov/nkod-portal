type Props = {
    error: Error;
}

export default function ErrorAlert(props: Props)
{
    return <div>
        {props.error.name}
    </div>
}