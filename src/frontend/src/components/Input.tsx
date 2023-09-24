import { HTMLAttributes, useId } from "react";

type Props = 
{
    label: string;
} & HTMLAttributes<HTMLInputElement>

export default function Radio(props: Props)
{
    const id = useId();

    return <><label className="govuk-label" htmlFor={id}>
        {props.label}
    </label>
    <input className="govuk-input" id={id} type="text" {...props} /></>;
}