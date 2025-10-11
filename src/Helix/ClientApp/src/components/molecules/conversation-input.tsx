import { zodResolver } from "@hookform/resolvers/zod"
import { Send } from "lucide-react"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { Button } from "../ui/button"
import {
  Form,
  FormDescription,
  FormField,
  FormItem,
  FormMessage,
} from "../ui/form"
import { Textarea } from "../ui/textarea"

const conversationInputSchema = z.object({
  prompt: z.string().nonempty("Please enter a prompt to continue."),
})

interface ConversationInputProps {
  onSubmitPrompt: (data: z.infer<typeof conversationInputSchema>) => void | Promise<void>
}

export default function ConversationInput({
  onSubmitPrompt,
}: ConversationInputProps) {
  const form = useForm({
    resolver: zodResolver(conversationInputSchema),
    defaultValues: {
      prompt: "",
    },
  })

  const isMac = navigator.userAgent.toUpperCase().indexOf("MAC") >= 0
  const modifierKey = isMac ? "Cmd" : "Ctrl"

  const handleSubmit = async (data: z.infer<typeof conversationInputSchema>) => {
    await onSubmitPrompt(data)
    form.reset()
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if ((e.metaKey || e.ctrlKey) && e.key === "Enter") {
      e.preventDefault()
      form.handleSubmit(handleSubmit)()
    }
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} className="py-4">
      <Form {...form}>
        <FormField
          control={form.control}
          name="prompt"
          render={({ field }) => (
            <FormItem>
              <div className="relative">
                <Textarea
                  id={field.name}
                  {...field}
                  placeholder="Enter your prompt."
                  className="pr-12 pb-12"
                  onKeyDown={handleKeyDown}
                />
                <Button
                  type="submit"
                  size="icon"
                  variant={"secondary"}
                  className="absolute bottom-2 right-2"
                >
                  <Send className="h-4 w-4" />
                </Button>
              </div>
              <FormMessage />
              <FormDescription>
                Press{" "}
                <kbd className="rounded border bg-muted px-1 text-xs font-semibold">
                  {modifierKey}
                </kbd>{" "}
                +{" "}
                <kbd className="rounded border bg-muted px-1 text-xs font-semibold">
                  Enter
                </kbd>{" "}
                to submit.
              </FormDescription>
            </FormItem>
          )}
        />
      </Form>
    </form>
  )
}
