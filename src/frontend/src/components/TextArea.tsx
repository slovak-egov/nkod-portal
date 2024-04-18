import React, { TextareaHTMLAttributes, forwardRef } from "react";

type Props = TextareaHTMLAttributes<HTMLTextAreaElement>

const TextArea = forwardRef<HTMLTextAreaElement, Props>((props, ref) => {
    return <textarea className="govuk-textarea" ref={ref} {...props} />;
});

TextArea.defaultProps = {
    rows: 5
}

export default TextArea;
// import { TextareaHTMLAttributes } from "react";
//
// type Props = TextareaHTMLAttributes<HTMLTextAreaElement>
//
// export default function TextArea(props: Props)
// {
//     return <textarea className="govuk-textarea" {...props} />;
// }
//
// TextArea.defaultProps = {
//     rows: 5
// }
