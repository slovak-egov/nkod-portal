import { HTMLAttributes, useId } from "react";

interface IProps extends HTMLAttributes<HTMLInputElement>
{
    label: string;
}

export default function Radio(props: IProps)
{
    const id = useId();

    return <><label className="govuk-label" htmlFor={id}>
        {props.label}
    </label>
    <input className="govuk-input" id={id} type="text" {...props} /></>;
}