import { InputHTMLAttributes } from "react";

type Props = InputHTMLAttributes<HTMLInputElement>

export default function BaseInput(props: Props)
{
    return <input className="govuk-input" {...props} />;
}

BaseInput.defaultProps = {
    type: 'text'
}