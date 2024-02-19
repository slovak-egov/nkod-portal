import { InputHTMLAttributes, useId } from "react";

type Props = 
{
    label: string;
} & InputHTMLAttributes<HTMLInputElement>

export default function Input(props: Props)
{
    const id = useId();

    return <><label className="govuk-label" htmlFor={id}>
        {props.label}
    </label>
    <input className="govuk-input" id={id} type="text" {...props} /></>;
}