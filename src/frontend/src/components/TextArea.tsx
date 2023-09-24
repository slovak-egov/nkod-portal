import { TextareaHTMLAttributes } from "react";

type Props = TextareaHTMLAttributes<HTMLTextAreaElement>

export default function TextArea(props: Props)
{
    return <textarea className="govuk-textarea" {...props} />;
}

TextArea.defaultProps = {
    rows: 5
}