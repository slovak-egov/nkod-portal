import { InputHTMLAttributes, useId } from "react";

type Props = 
{
    label: string;
} & InputHTMLAttributes<HTMLInputElement>

export default function Radio(props: Props)
{
    const id = useId();

    return <div className="govuk-radios__item">
            <input className="govuk-radios__input" type="radio" id={id} {...props} />
            <label className="govuk-label govuk-radios__label" htmlFor={id}>{props.label}</label>
        </div>;
}