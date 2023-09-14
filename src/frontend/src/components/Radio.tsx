import { InputHTMLAttributes, useId } from "react";

interface IProps extends InputHTMLAttributes<HTMLInputElement>
{
    label: string;
}

export default function Radio(props: IProps)
{
    const id = useId();

    return <div className="govuk-radios__item">
            <input className="govuk-radios__input" type="radio" id={id} {...props} />
            <label className="govuk-label govuk-radios__label" htmlFor={id}>{props.label}</label>
        </div>;
}